\set ON_ERROR_STOP on
SET client_min_messages = warning;

SELECT set_config('ucbhold.app_role', :'app_role', false);

DO $$
DECLARE
    app_role text := current_setting('ucbhold.app_role');
    table_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = app_role) THEN
        RAISE EXCEPTION 'El rol % no existe. Créalo con LOGIN, NOSUPERUSER y NOBYPASSRLS.', app_role;
    END IF;

    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = app_role AND (rolsuper OR rolbypassrls)) THEN
        RAISE EXCEPTION 'El rol % no puede ser superusuario ni tener BYPASSRLS.', app_role;
    END IF;

    EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', current_database(), app_role);
    EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', app_role);
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO %I', app_role);
    EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO %I', app_role);
    EXECUTE format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO %I', app_role);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I', app_role);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO %I', app_role);

    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'hangfire') THEN
        EXECUTE format('ALTER SCHEMA hangfire OWNER TO %I', app_role);
        FOR table_name IN SELECT tablename FROM pg_tables WHERE schemaname = 'hangfire' LOOP
            EXECUTE format('ALTER TABLE hangfire.%I OWNER TO %I', table_name, app_role);
        END LOOP;
    ELSE
        EXECUTE format('GRANT CREATE ON DATABASE %I TO %I', current_database(), app_role);
    END IF;
END
$$;

CREATE OR REPLACE FUNCTION public.app_carnet() RETURNS text
    LANGUAGE sql STABLE
    AS $$ SELECT NULLIF(current_setting('app.carnet', true), '') $$;

CREATE OR REPLACE FUNCTION public.app_rol() RETURNS text
    LANGUAGE sql STABLE
    AS $$ SELECT COALESCE(NULLIF(current_setting('app.rol', true), ''), 'ninguno') $$;

CREATE OR REPLACE FUNCTION public.app_es_gestion() RETURNS boolean
    LANGUAGE sql STABLE
    AS $$ SELECT public.app_rol() IN ('sistema', 'administrador', 'administrador_laboratorio') $$;

CREATE OR REPLACE FUNCTION public.app_es_anonimo() RETURNS boolean
    LANGUAGE sql STABLE
    AS $$ SELECT public.app_rol() = 'anonimo' $$;

CREATE OR REPLACE FUNCTION public.app_es_propietario(carnet text) RETURNS boolean
    LANGUAGE sql STABLE
    AS $$ SELECT carnet IS NOT NULL AND carnet = public.app_carnet() $$;

ALTER TABLE public.usuarios ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.usuarios FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS usuarios_lectura ON public.usuarios;
DROP POLICY IF EXISTS usuarios_registro ON public.usuarios;
DROP POLICY IF EXISTS usuarios_edicion ON public.usuarios;
DROP POLICY IF EXISTS usuarios_eliminacion ON public.usuarios;
CREATE POLICY usuarios_lectura ON public.usuarios FOR SELECT
    USING (public.app_rol() <> 'ninguno');
CREATE POLICY usuarios_registro ON public.usuarios FOR INSERT
    WITH CHECK (public.app_es_gestion() OR public.app_es_anonimo());
CREATE POLICY usuarios_edicion ON public.usuarios FOR UPDATE
    USING (public.app_es_gestion() OR public.app_es_anonimo() OR public.app_es_propietario(carnet))
    WITH CHECK (public.app_es_gestion() OR public.app_es_anonimo() OR public.app_es_propietario(carnet));
CREATE POLICY usuarios_eliminacion ON public.usuarios FOR DELETE
    USING (public.app_es_gestion());

ALTER TABLE public.prestamos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.prestamos FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS prestamos_lectura ON public.prestamos;
DROP POLICY IF EXISTS prestamos_creacion ON public.prestamos;
DROP POLICY IF EXISTS prestamos_edicion ON public.prestamos;
DROP POLICY IF EXISTS prestamos_eliminacion ON public.prestamos;
CREATE POLICY prestamos_lectura ON public.prestamos FOR SELECT
    USING (public.app_rol() <> 'ninguno');
CREATE POLICY prestamos_creacion ON public.prestamos FOR INSERT
    WITH CHECK (public.app_es_gestion() OR public.app_es_propietario(carnet));
CREATE POLICY prestamos_edicion ON public.prestamos FOR UPDATE
    USING (public.app_es_gestion() OR public.app_es_propietario(carnet))
    WITH CHECK (public.app_es_gestion() OR public.app_es_propietario(carnet));
CREATE POLICY prestamos_eliminacion ON public.prestamos FOR DELETE
    USING (public.app_es_gestion());

ALTER TABLE public.contratos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.contratos FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS contratos_lectura ON public.contratos;
DROP POLICY IF EXISTS contratos_escritura ON public.contratos;
CREATE POLICY contratos_lectura ON public.contratos FOR SELECT
    USING (
        public.app_es_gestion()
        OR EXISTS (
            SELECT 1 FROM public.prestamos p
            WHERE p.id_contrato = contratos.id AND public.app_es_propietario(p.carnet)
        )
    );
CREATE POLICY contratos_escritura ON public.contratos FOR ALL
    USING (public.app_es_gestion())
    WITH CHECK (public.app_es_gestion());

ALTER TABLE public.notificaciones ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.notificaciones FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS notificaciones_propias ON public.notificaciones;
CREATE POLICY notificaciones_propias ON public.notificaciones FOR ALL
    USING (public.app_es_gestion() OR public.app_es_propietario(carnet_usuario))
    WITH CHECK (public.app_es_gestion() OR public.app_es_propietario(carnet_usuario));

ALTER TABLE public.avisos_disponibilidad ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.avisos_disponibilidad FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS avisos_propios ON public.avisos_disponibilidad;
CREATE POLICY avisos_propios ON public.avisos_disponibilidad FOR ALL
    USING (public.app_es_gestion() OR public.app_es_propietario(carnet_usuario))
    WITH CHECK (public.app_es_gestion() OR public.app_es_propietario(carnet_usuario));

ALTER TABLE public.audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.audit_logs FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS audit_logs_gestion ON public.audit_logs;
CREATE POLICY audit_logs_gestion ON public.audit_logs FOR ALL
    USING (public.app_es_gestion())
    WITH CHECK (public.app_es_gestion());

ALTER TABLE public.codigos_autenticacion ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.codigos_autenticacion FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS codigos_autenticacion_flujo ON public.codigos_autenticacion;
CREATE POLICY codigos_autenticacion_flujo ON public.codigos_autenticacion FOR ALL
    USING (public.app_rol() IN ('anonimo', 'sistema'))
    WITH CHECK (public.app_rol() IN ('anonimo', 'sistema'));
