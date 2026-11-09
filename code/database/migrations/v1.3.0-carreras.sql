BEGIN;
ALTER TABLE public.carreras ADD COLUMN IF NOT EXISTS codigo character varying(10);
INSERT INTO public.carreras (nombre)
SELECT 'Ingeniería Mecatrónica'
WHERE NOT EXISTS (SELECT 1 FROM public.carreras WHERE nombre ILIKE '%mecatr%' AND NOT estado_eliminado);
WITH base AS (
    SELECT id_carrera, estado_eliminado, nombre ILIKE '%mecatr%' AS mecatronica,
           CASE WHEN nombre ILIKE '%mecatr%' THEN 'IMT'
                ELSE left(upper(regexp_replace(translate(regexp_replace(trim(nombre), '^.*\s', ''), 'áéíóúüñÁÉÍÓÚÜÑ', 'aeiouunAEIOUUN'), '[^A-Za-z0-9]', '', 'g')), 3)
           END AS codigo
    FROM public.carreras
    WHERE codigo IS NULL
), numerado AS (
    SELECT id_carrera, mecatronica, estado_eliminado,
           CASE WHEN length(codigo) < 2 THEN 'CAR' ELSE codigo END AS codigo,
           row_number() OVER (PARTITION BY CASE WHEN length(codigo) < 2 THEN 'CAR' ELSE codigo END ORDER BY mecatronica DESC, estado_eliminado, id_carrera) AS n
    FROM base
)
UPDATE public.carreras c
SET codigo = CASE WHEN n.n = 1 THEN n.codigo ELSE n.codigo || n.n END
FROM numerado n
WHERE c.id_carrera = n.id_carrera;
ALTER TABLE public.carreras ALTER COLUMN codigo SET NOT NULL;
ALTER TABLE public.carreras DROP CONSTRAINT IF EXISTS ck_carreras_codigo;
ALTER TABLE public.carreras ADD CONSTRAINT ck_carreras_codigo CHECK (((codigo)::text ~ '^[A-Z0-9]{2,10}$'::text));
CREATE UNIQUE INDEX IF NOT EXISTS unique_carreras_codigo ON public.carreras USING btree (codigo) WHERE (NOT estado_eliminado);
ALTER TABLE public.equipos ADD COLUMN IF NOT EXISTS id_carrera integer;
ALTER TABLE public.equipos DROP CONSTRAINT IF EXISTS equipos_id_carrera_fkey;
ALTER TABLE ONLY public.equipos ADD CONSTRAINT equipos_id_carrera_fkey FOREIGN KEY (id_carrera) REFERENCES public.carreras(id_carrera) ON DELETE RESTRICT;
CREATE INDEX IF NOT EXISTS idx_equipos_id_carrera ON public.equipos USING btree (id_carrera);
UPDATE public.equipos
SET id_carrera = (SELECT id_carrera FROM public.carreras WHERE codigo = 'IMT' AND NOT estado_eliminado)
WHERE id_carrera IS NULL;
COMMIT;
