--
-- PostgreSQL database dump
--

-- Dumped from database version 17.4
-- Dumped by pg_dump version 17.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: hangfire; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA hangfire;


--
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


--
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA public IS '';


--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


--
-- Name: estado_disponibilidad; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.estado_disponibilidad AS ENUM (
    'disponible',
    'mantenimiento',
    'ocupado'
);


--
-- Name: estado_equipo; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.estado_equipo AS ENUM (
    'operativo',
    'parcialmente_operativo',
    'inoperativo',
    'en_mantenimiento'
);


--
-- Name: estado_prestamo; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.estado_prestamo AS ENUM (
    'pendiente',
    'rechazado',
    'aprobado',
    'activo',
    'finalizado',
    'cancelado',
    'atrasado'
);


--
-- Name: tipo_mantenimiento; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.tipo_mantenimiento AS ENUM (
    'correctivo',
    'preventivo'
);


--
-- Name: tipo_usuario; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.tipo_usuario AS ENUM (
    'docente',
    'administrador',
    'estudiante',
    'administrativo',
    'administrador_laboratorio'
);


--
-- Name: actualizar_accesorio(integer, character varying, character varying, character varying, integer, text, double precision, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_accesorio(IN p_id_accesorio_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_codigo_imt_nuevo integer DEFAULT NULL::integer, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_precio_nuevo double precision DEFAULT NULL::double precision, IN p_url_data_sheet_nueva text DEFAULT NULL::text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_equipo_para_actualizar INTEGER;
    v_accesorio_existe BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1
        FROM public.accesorios
        WHERE id_accesorio = p_id_accesorio_actualizar AND estado_eliminado = FALSE
    ) INTO v_accesorio_existe;

    IF NOT v_accesorio_existe THEN
        RAISE EXCEPTION 'No se encontró un accesorio activo con ID = % para actualizar.', p_id_accesorio_actualizar;
    END IF;

    IF p_codigo_imt_nuevo IS NOT NULL THEN
        SELECT e.id_equipo
          INTO v_id_equipo_para_actualizar
          FROM public.equipos AS e
         WHERE e.codigo_imt = p_codigo_imt_nuevo
           AND e.estado_eliminado = FALSE;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'No se encontró un equipo activo con código IMT = % para asociar al accesorio.', p_codigo_imt_nuevo;
        END IF;
    END IF;

    UPDATE public.accesorios
       SET
           nombre         = COALESCE(p_nombre_nuevo, nombre),
           modelo         = COALESCE(p_modelo_nuevo, modelo),
           tipo           = COALESCE(p_tipo_nuevo, tipo),
           id_equipo      = COALESCE(v_id_equipo_para_actualizar, id_equipo),
           descripcion    = COALESCE(p_descripcion_nueva, descripcion),
           precio         = COALESCE(p_precio_nuevo, precio),
           url_data_sheet = COALESCE(p_url_data_sheet_nueva, url_data_sheet)
     WHERE id_accesorio = p_id_accesorio_actualizar; 
	 
EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Error de violación de unicidad al actualizar el accesorio. Verifique que los nuevos datos no dupliquen información existente: %', SQLERRM;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar el accesorio: %', SQLERRM;
END;
$$;


--
-- Name: actualizar_cantidad_grupos_equipos(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_cantidad_grupos_equipos()
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_grupo_id integer;
    v_cantidad_actual integer;
    v_cantidad_esperada integer;
    v_total_actualizado integer := 0;
BEGIN
    -- Iterar sobre todos los grupos de equipos
    FOR v_grupo_id IN SELECT id_grupo_equipo FROM public.grupos_equipos ORDER BY id_grupo_equipo LOOP
        
        -- Contar equipos activos (no eliminados) en este grupo
        SELECT COUNT(*)
        INTO v_cantidad_esperada
        FROM public.equipos
        WHERE id_grupo_equipo = v_grupo_id
          AND estado_eliminado = FALSE;
        
        -- Obtener la cantidad actual registrada
        SELECT cantidad
        INTO v_cantidad_actual
        FROM public.grupos_equipos
        WHERE id_grupo_equipo = v_grupo_id;
        
        -- Si no coinciden, actualizar
        IF v_cantidad_actual != v_cantidad_esperada THEN
            UPDATE public.grupos_equipos
            SET cantidad = v_cantidad_esperada
            WHERE id_grupo_equipo = v_grupo_id;
            
            v_total_actualizado := v_total_actualizado + 1;
            
            RAISE NOTICE 'Grupo % actualizado: % -> % equipos', 
                v_grupo_id, v_cantidad_actual, v_cantidad_esperada;
        END IF;
    END LOOP;
    
    RAISE NOTICE 'Total de grupos actualizados: %', v_total_actualizado;
END;
$$;


--
-- Name: actualizar_carrera(integer, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_carrera(IN p_id_carrera_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_carrera_existe BOOLEAN;
    v_nombre_actual character varying;
    v_id_existente integer;
BEGIN
    SELECT EXISTS (
        SELECT 1
        FROM public.carreras
        WHERE id_carrera = p_id_carrera_actualizar AND estado_eliminado = FALSE
    ) INTO v_carrera_existe;

    IF NOT v_carrera_existe THEN
        RAISE EXCEPTION 'No se encontró una carrera activa con ID = % para actualizar.', p_id_carrera_actualizar;
    END IF;

    IF p_nombre_nuevo IS NOT NULL THEN
        IF trim(p_nombre_nuevo) = '' THEN
            RAISE EXCEPTION 'El nuevo nombre de la carrera no puede estar vacío.';
        END IF;

        -- Verificar si ya existe una carrera activa con ese nombre (diferente al ID actual)
        IF EXISTS (
            SELECT 1
            FROM public.carreras
            WHERE nombre = p_nombre_nuevo
              AND estado_eliminado = FALSE
              AND id_carrera <> p_id_carrera_actualizar
        ) THEN
            RAISE EXCEPTION 'Ya existe otra carrera con el nombre "%".', p_nombre_nuevo;
        END IF;

        -- Si existe una carrera eliminada con el mismo nombre, reactivarla y eliminar lógicamente la actual
        SELECT id_carrera INTO v_id_existente
        FROM public.carreras
        WHERE nombre = p_nombre_nuevo
          AND estado_eliminado = TRUE
        LIMIT 1;

        IF v_id_existente IS NOT NULL THEN
            -- Reactivar la carrera eliminada
            UPDATE public.carreras
            SET estado_eliminado = FALSE
            WHERE id_carrera = v_id_existente;

            -- Eliminar lógicamente la carrera actual
            UPDATE public.carreras
            SET estado_eliminado = TRUE
            WHERE id_carrera = p_id_carrera_actualizar;

            RETURN;
        END IF;
    END IF;

    -- Actualización normal si no hay conflictos de nombre
    UPDATE public.carreras
    SET nombre = COALESCE(p_nombre_nuevo, nombre)
    WHERE id_carrera = p_id_carrera_actualizar
      AND estado_eliminado = FALSE;

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe otra carrera con el nombre "%".', COALESCE(p_nombre_nuevo, (SELECT nombre FROM public.carreras WHERE id_carrera = p_id_carrera_actualizar));
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar la carrera: %', SQLERRM;
END;
$$;


--
-- Name: actualizar_categoria(integer, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_categoria(IN p_id_categoria_actualizar integer, IN p_nombre_nuevo_raw character varying DEFAULT NULL::character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_categoria_existe   BOOLEAN;
    v_nombre_nuevo_procesado TEXT;
    v_id_existente       integer;
BEGIN
    -- 1) Verificar que la categoría exista y esté activa
    SELECT EXISTS (
        SELECT 1
        FROM public.categorias
        WHERE id_categoria = p_id_categoria_actualizar
          AND estado_eliminado = FALSE
    ) INTO v_categoria_existe;

    IF NOT v_categoria_existe THEN
        RAISE EXCEPTION 'No se encontró una categoría activa con ID = % para actualizar.', p_id_categoria_actualizar;
    END IF;

    -- 2) Procesar nuevo nombre si se proporcionó
    IF p_nombre_nuevo_raw IS NOT NULL THEN
        v_nombre_nuevo_procesado := TRIM(both ' ' FROM p_nombre_nuevo_raw);

        IF v_nombre_nuevo_procesado = '' THEN
            RAISE EXCEPTION 'El nuevo nombre de la categoría no puede estar vacío.';
        END IF;

        -- 3) Verificar conflicto con otra categoría activa distinta
        IF EXISTS (
            SELECT 1
            FROM public.categorias
            WHERE nombre = v_nombre_nuevo_procesado
              AND estado_eliminado = FALSE
              AND id_categoria <> p_id_categoria_actualizar
        ) THEN
            RAISE EXCEPTION 'Ya existe otra categoría con el nombre "%".', v_nombre_nuevo_procesado;
        END IF;

        -- 4) Si hay una categoría eliminada con el mismo nombre, reactivar esa
        SELECT id_categoria
        INTO   v_id_existente
        FROM   public.categorias
        WHERE  nombre = v_nombre_nuevo_procesado
          AND  estado_eliminado = TRUE
        LIMIT 1;

        IF v_id_existente IS NOT NULL THEN
            -- Reactivar la existente
            UPDATE public.categorias
               SET estado_eliminado = FALSE
             WHERE id_categoria = v_id_existente;

            -- Eliminar lógicamente la actual
            UPDATE public.categorias
               SET estado_eliminado = TRUE
             WHERE id_categoria = p_id_categoria_actualizar;

            RETURN;
        END IF;
    ELSE
        v_nombre_nuevo_procesado := NULL;
    END IF;

    -- 5) Actualización normal si no hubo reactivación ni conflicto
    UPDATE public.categorias
       SET nombre = COALESCE(v_nombre_nuevo_procesado, nombre)
     WHERE id_categoria = p_id_categoria_actualizar
       AND estado_eliminado = FALSE;

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe otra categoría con el nombre "%".',
            COALESCE(
              v_nombre_nuevo_procesado,
              (SELECT nombre FROM public.categorias WHERE id_categoria = p_id_categoria_actualizar)
            );
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar la categoría: %', SQLERRM;
END;
$$;


--
-- Name: actualizar_componente(integer, character varying, character varying, character varying, integer, text, double precision, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_componente(IN p_id_componente_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_codigo_imt_nuevo integer DEFAULT NULL::integer, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_precio_referencia_nuevo double precision DEFAULT NULL::double precision, IN p_url_data_sheet_nueva text DEFAULT NULL::text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_equipo_para_actualizar INTEGER; 
    v_componente_existe BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1
        FROM public.componentes
        WHERE id_componente = p_id_componente_actualizar AND estado_eliminado = FALSE
    ) INTO v_componente_existe;

    IF NOT v_componente_existe THEN
        RAISE EXCEPTION 'No se encontró un componente activo con ID = % para actualizar.', p_id_componente_actualizar;
    END IF;

     IF p_codigo_imt_nuevo IS NOT NULL THEN
        SELECT e.id_equipo
          INTO v_id_equipo_para_actualizar
          FROM public.equipos AS e
         WHERE e.codigo_imt = p_codigo_imt_nuevo
           AND e.estado_eliminado = FALSE;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'No se encontró un equipo activo con código IMT = % para asociar al componente.', p_codigo_imt_nuevo;
        END IF;
    END IF; 
   
    UPDATE public.componentes
       SET
           nombre            = COALESCE(p_nombre_nuevo, nombre),
           modelo            = COALESCE(p_modelo_nuevo, modelo),
           tipo              = COALESCE(p_tipo_nuevo, tipo),
           id_equipo         = COALESCE(v_id_equipo_para_actualizar, id_equipo),
           descripcion       = COALESCE(p_descripcion_nueva, descripcion),
           precio_referencia = COALESCE(p_precio_referencia_nuevo, precio_referencia),
           url_data_sheet    = COALESCE(p_url_data_sheet_nueva, url_data_sheet)
     WHERE id_componente = p_id_componente_actualizar;

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Error de violación de unicidad al actualizar el componente. Verifique que los nuevos datos no dupliquen información existente (ej. nombre+modelo+id_equipo si existe tal restricción): %', SQLERRM;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar el componente: %', SQLERRM;
END;
$$;


--
-- Name: actualizar_empresa_mantenimiento(integer, character varying, character varying, character varying, character varying, text, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_empresa_mantenimiento(IN p_id_empresa_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_nombre_responsable_nuevo character varying DEFAULT NULL::character varying, IN p_apellido_responsable_nuevo character varying DEFAULT NULL::character varying, IN p_telefono_nuevo character varying DEFAULT NULL::character varying, IN p_direccion_nueva text DEFAULT NULL::text, IN p_nit_nuevo character varying DEFAULT NULL::character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_empresa_existe   BOOLEAN;
    v_nombre_trimmed   TEXT;
BEGIN
    -- 1) Verificar que la empresa exista y esté activa
    SELECT EXISTS (
        SELECT 1
          FROM public.empresas_mantenimiento
         WHERE id_empresa_mantenimiento = p_id_empresa_actualizar
           AND estado_eliminado = FALSE
    ) INTO v_empresa_existe;

    IF NOT v_empresa_existe THEN
        RAISE EXCEPTION 'No se encontró una empresa de mantenimiento activa con ID = % para actualizar.', p_id_empresa_actualizar;
    END IF;

    -- 2) Lógica de duplicados solo para el nombre
    IF p_nombre_nuevo IS NOT NULL THEN
        v_nombre_trimmed := TRIM(both ' ' FROM p_nombre_nuevo);

        -- 2a) Reactivar si existe eliminada lógicamente
        UPDATE public.empresas_mantenimiento
           SET estado_eliminado = FALSE
         WHERE nombre = v_nombre_trimmed
           AND estado_eliminado = TRUE;
        IF FOUND THEN
            -- Eliminar lógicamente la actual y terminar
            UPDATE public.empresas_mantenimiento
               SET estado_eliminado = TRUE
             WHERE id_empresa_mantenimiento = p_id_empresa_actualizar;
            RETURN;
        END IF;

        -- 2b) Verificar si otro registro activo ya tiene ese nombre
        IF EXISTS (
            SELECT 1
              FROM public.empresas_mantenimiento
             WHERE nombre = v_nombre_trimmed
               AND estado_eliminado = FALSE
               AND id_empresa_mantenimiento <> p_id_empresa_actualizar
        ) THEN
            RAISE EXCEPTION 'Error de violación de unicidad al actualizar la empresa de mantenimiento. Verifique que los nuevos datos (ej. NIT o nombre) no dupliquen información existente: %', SQLERRM;
        END IF;
    END IF;

    -- 3) Actualización normal de todos los campos
    UPDATE public.empresas_mantenimiento
       SET
           nombre               = COALESCE(v_nombre_trimmed, nombre),
           nombre_responsable   = COALESCE(p_nombre_responsable_nuevo, nombre_responsable),
           apellido_responsable = COALESCE(p_apellido_responsable_nuevo, apellido_responsable),
           telefono             = COALESCE(p_telefono_nuevo, telefono),
           direccion            = COALESCE(p_direccion_nueva, direccion),
           nit                  = COALESCE(p_nit_nuevo, nit)
     WHERE id_empresa_mantenimiento = p_id_empresa_actualizar
       AND estado_eliminado = FALSE;

EXCEPTION
    WHEN unique_violation THEN
         RAISE EXCEPTION 'Error de violación de unicidad al actualizar la empresa de mantenimiento. Verifique que los nuevos datos (ej. NIT o nombre) no dupliquen información existente: %', SQLERRM;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar la empresa de mantenimiento: %', SQLERRM;
END;
$$;


--
-- Name: actualizar_equipo(integer, character varying, character varying, character varying, character varying, text, character varying, character varying, character varying, double precision, integer, character varying, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_equipo(IN p_id_equipo_actualizar integer, IN p_nombre_grupo_equipo_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_grupo_equipo_nuevo character varying DEFAULT NULL::character varying, IN p_marca_grupo_equipo_nuevo character varying DEFAULT NULL::character varying, IN p_codigo_ucb_nuevo character varying DEFAULT NULL::character varying, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_numero_serial_nuevo character varying DEFAULT NULL::character varying, IN p_ubicacion_nueva character varying DEFAULT NULL::character varying, IN p_procedencia_nueva character varying DEFAULT NULL::character varying, IN p_costo_referencia_nuevo double precision DEFAULT NULL::double precision, IN p_tiempo_maximo_prestamo_nuevo integer DEFAULT NULL::integer, IN p_nombre_gavetero_nuevo character varying DEFAULT NULL::character varying, IN p_estado_equipo_nuevo character varying DEFAULT NULL::character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_grupo_equipo_actual        INTEGER;
    v_id_categoria_actual           INTEGER;
    v_id_grupo_equipo_para_actualizar INTEGER;
    v_id_categoria_para_actualizar  INTEGER;
    v_id_gavetero_para_actualizar     INTEGER;
BEGIN
    SELECT
        e.id_grupo_equipo,
        ge.id_categoria
    INTO
        v_id_grupo_equipo_actual,
        v_id_categoria_actual
    FROM public.equipos e
    JOIN public.grupos_equipos ge ON e.id_grupo_equipo = ge.id_grupo_equipo
    WHERE e.id_equipo = p_id_equipo_actualizar AND e.estado_eliminado = FALSE AND ge.estado_eliminado = FALSE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un equipo activo con ID = % o su grupo asociado no existe/está eliminado.', p_id_equipo_actualizar;
    END IF;

    IF p_nombre_grupo_equipo_nuevo IS NOT NULL and p_modelo_grupo_equipo_nuevo is not null and p_marca_grupo_equipo_nuevo is not null THEN
        SELECT ge.id_grupo_equipo, ge.id_categoria
          INTO v_id_grupo_equipo_para_actualizar, v_id_categoria_para_actualizar
          FROM public.grupos_equipos AS ge
         WHERE ge.nombre           = p_nombre_grupo_equipo_nuevo
		   AND ge.modelo 			= p_modelo_grupo_equipo_nuevo
		   AND ge.marca				= p_marca_grupo_equipo_nuevo
           AND ge.estado_eliminado = FALSE;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'No se encontró el grupo de equipos con nombre = % para asociar.', p_nombre_grupo_equipo_nuevo;
        END IF;
    ELSE
        v_id_grupo_equipo_para_actualizar := NULL;
    END IF;

    IF p_nombre_gavetero_nuevo IS NOT NULL THEN
        SELECT g.id_gavetero
          INTO v_id_gavetero_para_actualizar
          FROM public.gaveteros AS g
         WHERE g.nombre            = p_nombre_gavetero_nuevo
           AND g.estado_eliminado  = FALSE;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'No se encontró el gavetero con nombre = % para asociar.', p_nombre_gavetero_nuevo;
        END IF;
    ELSE
        v_id_gavetero_para_actualizar := NULL;
    END IF;

    IF p_estado_equipo_nuevo IS NOT NULL THEN
        IF lower(p_estado_equipo_nuevo) NOT IN ('operativo', 'inoperativo', 'parcialmente_operativo') THEN
            RAISE EXCEPTION 'Valor inválido para estado_equipo: "%". Debe ser ''operativo'', ''inoperativo'', o ''parcialmente_operativo''.', p_estado_equipo_nuevo;
        END IF;
    END IF;

    UPDATE public.equipos
       SET
           id_grupo_equipo     = COALESCE(v_id_grupo_equipo_para_actualizar, id_grupo_equipo),
           id_gavetero         = COALESCE(v_id_gavetero_para_actualizar, id_gavetero),
           codigo_ucb          = COALESCE(p_codigo_ucb_nuevo, codigo_ucb),
           descripcion         = COALESCE(p_descripcion_nueva, descripcion),
           numero_serial       = COALESCE(p_numero_serial_nuevo, numero_serial),
           ubicacion           = COALESCE(p_ubicacion_nueva, ubicacion),
           procedencia         = COALESCE(p_procedencia_nueva, procedencia),
           costo_referencia    = COALESCE(p_costo_referencia_nuevo, costo_referencia),
           tiempo_max_prestamo = COALESCE(p_tiempo_maximo_prestamo_nuevo, tiempo_max_prestamo),
           estado_equipo       = COALESCE(CAST(lower(p_estado_equipo_nuevo) AS public.estado_equipo), estado_equipo) -- CORRECCIÓN AQUÍ
     WHERE id_equipo = p_id_equipo_actualizar;

EXCEPTION
    WHEN unique_violation THEN
        IF SQLERRM LIKE '%unique_codigo_imt%' THEN
            RAISE EXCEPTION 'Error: El código IMT generado ("%") para el nuevo grupo de equipo ya está en uso por otro equipo. Esto ocurre si se cambió a una categoría diferente y el código IMT resultante ya existe. (Detalle: %)', v_codigo_imt_a_usar, SQLERRM;
        ELSIF SQLERRM LIKE '%equipos_codigo_ucb_key%' OR SQLERRM LIKE '%unique_codigo_ucb%' THEN
            RAISE EXCEPTION 'Error: El código UCB "%" ya existe para otro equipo. (Detalle: %)', p_codigo_ucb_nuevo, SQLERRM;
        ELSIF SQLERRM LIKE '%equipos_numero_serial_key%' OR SQLERRM LIKE '%unique_numero_serial%' THEN
            RAISE EXCEPTION 'Error: El número de serie "%" ya existe para otro equipo. (Detalle: %)', p_numero_serial_nuevo, SQLERRM;
        ELSE
            RAISE EXCEPTION 'Error de violación de unicidad al actualizar el equipo. (Detalle: %)', SQLERRM;
        END IF;
    WHEN OTHERS THEN
         RAISE EXCEPTION 'Error inesperado al actualizar el equipo: % (SQLSTATE: %)', SQLERRM, SQLSTATE;
END;
$$;


--
-- Name: actualizar_estado_prestamo(integer, public.estado_prestamo); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_estado_prestamo(IN p_id_prestamo integer, IN p_estado_prestamo_input public.estado_prestamo)
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- 1. Validar que el nuevo estado esté dentro de los permitidos
    IF p_estado_prestamo_input NOT IN (
        'pendiente',
        'rechazado',
        'aprobado',
        'activo',
        'finalizado',
        'cancelado'
    ) THEN
        RAISE EXCEPTION 
            'Estado inválido: “%”. Solo se permiten pendiente, rechazado, aprobado, activo, finalizado o cancelado.',
            p_estado_prestamo_input
        USING ERRCODE = '22023';  -- invalid_parameter_value
    END IF;

    -- 2. Intentar la actualización
    BEGIN
        UPDATE public.prestamos
           SET estado_prestamo = p_estado_prestamo_input
         WHERE id_prestamo    = p_id_prestamo;

        -- 3. Verificar que realmente se haya actualizado alguna fila
        IF NOT FOUND THEN
            RAISE EXCEPTION 
                'No existe préstamo con id % o el estado ya era el mismo.',
                p_id_prestamo
            USING ERRCODE = 'P0002';  -- no_data_found-like
        END IF;

    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 
                'Error inesperado (%s) al actualizar estado del préstamo %: %',
                SQLSTATE, p_id_prestamo, SQLERRM;
    END;
END;
$$;


--
-- Name: actualizar_gavetero(integer, character varying, character varying, character varying, double precision, double precision, double precision); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_gavetero(IN p_id_gavetero_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_nombre_mueble_nuevo character varying DEFAULT NULL::character varying, IN p_longitud_nueva double precision DEFAULT NULL::double precision, IN p_profundidad_nueva double precision DEFAULT NULL::double precision, IN p_altura_nueva double precision DEFAULT NULL::double precision)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_mueble_para_actualizar INTEGER;
    v_gavetero_existe BOOLEAN;
    v_nombre_actual character varying;
BEGIN
    SELECT g.nombre, TRUE
      INTO v_nombre_actual, v_gavetero_existe
      FROM public.gaveteros g
     WHERE g.id_gavetero = p_id_gavetero_actualizar AND g.estado_eliminado = FALSE;

    IF NOT v_gavetero_existe THEN
        RAISE EXCEPTION 'No se encontró un gavetero activo con ID = % para actualizar.', p_id_gavetero_actualizar;
    END IF;

    IF p_nombre_mueble_nuevo IS NOT NULL THEN
        SELECT m.id_mueble
          INTO v_id_mueble_para_actualizar
          FROM public.muebles AS m
         WHERE m.nombre           = p_nombre_mueble_nuevo
           AND m.estado_eliminado = FALSE;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'No se encontró el mueble activo con nombre = % para asociar al gavetero.', p_nombre_mueble_nuevo;
        END IF;
    ELSE
        v_id_mueble_para_actualizar := NULL; 
    END IF;

 
    IF p_nombre_nuevo IS NOT NULL AND p_nombre_nuevo IS DISTINCT FROM v_nombre_actual THEN
        IF EXISTS (
            SELECT 1
              FROM public.gaveteros
             WHERE nombre = p_nombre_nuevo
               AND estado_eliminado = FALSE
               
        ) THEN
            RAISE EXCEPTION 'Ya existe otro gavetero activo con el nombre = %.', p_nombre_nuevo;
        END IF;
    END IF;

    UPDATE public.gaveteros
       SET
           nombre      = COALESCE(p_nombre_nuevo, nombre),
           tipo        = COALESCE(p_tipo_nuevo, tipo),
           id_mueble   = COALESCE(v_id_mueble_para_actualizar, id_mueble),
           longitud    = COALESCE(p_longitud_nueva, longitud),
           profundidad = COALESCE(p_profundidad_nueva, profundidad),
           altura      = COALESCE(p_altura_nueva, altura)
     WHERE id_gavetero = p_id_gavetero_actualizar;

EXCEPTION
    WHEN unique_violation THEN
         RAISE EXCEPTION 'Violación de unicidad al intentar actualizar el gavetero con nombre "%". (Detalle: %)', COALESCE(p_nombre_nuevo, v_nombre_actual), SQLERRM;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar el gavetero: %', SQLERRM;
END;
$$;


--
-- Name: actualizar_grupo_equipo(integer, character varying, character varying, character varying, text, character varying, text, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_grupo_equipo(IN p_id_grupo_equipo_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_nuevo character varying DEFAULT NULL::character varying, IN p_marca_nueva character varying DEFAULT NULL::character varying, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_nombre_categoria_nuevo character varying DEFAULT NULL::character varying, IN p_url_data_sheet_nuevo text DEFAULT NULL::text, IN p_url_imagen_nuevo text DEFAULT NULL::text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_categoria_actual            INTEGER; 
    v_id_categoria_para_actualizar   INTEGER;
    v_grupo_equipo_existe            BOOLEAN;
    v_nombre_actual                  character varying;
    v_modelo_actual                  character varying;
    v_marca_actual                   character varying;
    v_nombre_final_para_verificar    character varying;
    v_modelo_final_para_verificar    character varying;
    v_marca_final_para_verificar     character varying;
BEGIN
    SELECT
        ge.nombre,
        ge.modelo,
        ge.marca,
        ge.id_categoria 
    INTO
        v_nombre_actual,
        v_modelo_actual,
        v_marca_actual,
        v_id_categoria_actual 
    FROM public.grupos_equipos ge
    WHERE ge.id_grupo_equipo = p_id_grupo_equipo_actualizar AND ge.estado_eliminado = FALSE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un grupo de equipos activo con ID = % para actualizar.', p_id_grupo_equipo_actualizar;
    END IF;

    IF p_nombre_categoria_nuevo IS NOT NULL THEN
        SELECT c.id_categoria
          INTO v_id_categoria_para_actualizar
          FROM public.categorias AS c
         WHERE c.nombre           = p_nombre_categoria_nuevo
           AND c.estado_eliminado = FALSE;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'No se encontró la categoría activa con nombre = "%" para asociar al grupo de equipos.', p_nombre_categoria_nuevo;
        END IF;
    ELSE

        v_id_categoria_para_actualizar := NULL;
    END IF;

    v_nombre_final_para_verificar := COALESCE(p_nombre_nuevo, v_nombre_actual);
    v_modelo_final_para_verificar := COALESCE(p_modelo_nuevo, v_modelo_actual);
    v_marca_final_para_verificar  := COALESCE(p_marca_nueva, v_marca_actual);

    IF (v_nombre_final_para_verificar IS DISTINCT FROM v_nombre_actual) OR
       (v_modelo_final_para_verificar IS DISTINCT FROM v_modelo_actual) OR
       (v_marca_final_para_verificar IS DISTINCT FROM v_marca_actual)
    THEN
        IF EXISTS (
            SELECT 1
              FROM public.grupos_equipos AS ge
             WHERE ge.nombre            = v_nombre_final_para_verificar
               AND ge.modelo            = v_modelo_final_para_verificar
               AND ge.marca             = v_marca_final_para_verificar
               AND ge.estado_eliminado  = FALSE
               AND ge.id_grupo_equipo   != p_id_grupo_equipo_actualizar 
        ) THEN
            RAISE EXCEPTION 'Ya existe otro grupo de equipos activo con la combinación: Nombre="%", Modelo="%", Marca="%".',
                v_nombre_final_para_verificar, v_modelo_final_para_verificar, v_marca_final_para_verificar;
        END IF;
    END IF;

    UPDATE public.grupos_equipos
       SET
           nombre         = COALESCE(p_nombre_nuevo, nombre),
           modelo         = COALESCE(p_modelo_nuevo, modelo),
           marca          = COALESCE(p_marca_nueva, marca),
           descripcion    = COALESCE(p_descripcion_nueva, descripcion),
           id_categoria   = COALESCE(v_id_categoria_para_actualizar, id_categoria), 
           url_data_sheet = COALESCE(p_url_data_sheet_nuevo, url_data_sheet),
           url_imagen     = COALESCE(p_url_imagen_nuevo, url_imagen)
     WHERE id_grupo_equipo = p_id_grupo_equipo_actualizar;

EXCEPTION
    WHEN unique_violation THEN
          RAISE EXCEPTION 'Error: La combinación Nombre="%", Modelo="%", Marca="%" ya está en uso por otro grupo de equipos. (Detalle: %)',
            v_nombre_final_para_verificar, v_modelo_final_para_verificar, v_marca_final_para_verificar, SQLERRM;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar el grupo de equipos: % (SQLSTATE: %)', SQLERRM, SQLSTATE;
END;
$$;


--
-- Name: actualizar_mueble(integer, character varying, character varying, double precision, character varying, double precision, double precision, double precision); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_mueble(IN p_id_mueble_actual integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_costo_nuevo double precision DEFAULT NULL::double precision, IN p_ubicacion_nueva character varying DEFAULT NULL::character varying, IN p_longitud_nueva double precision DEFAULT NULL::double precision, IN p_profundidad_nueva double precision DEFAULT NULL::double precision, IN p_altura_nueva double precision DEFAULT NULL::double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.muebles
     WHERE id_mueble = p_id_mueble_actual
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un mueble activo con ID = %.', p_id_mueble_actual;
    END IF;

    UPDATE public.muebles
       SET
           nombre      = COALESCE(p_nombre_nuevo, nombre),
           tipo        = COALESCE(p_tipo_nuevo, tipo),
           costo       = COALESCE(p_costo_nuevo, costo),
           ubicacion   = COALESCE(p_ubicacion_nueva, ubicacion),
           longitud    = COALESCE(p_longitud_nueva, longitud),
           profundidad = COALESCE(p_profundidad_nueva, profundidad),
           altura      = COALESCE(p_altura_nueva, altura)
     WHERE id_mueble = p_id_mueble_actual
       AND estado_eliminado = FALSE;

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Error: Ya existe un mueble con el nombre "%" en la base de datos.', p_nombre_nuevo;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar mueble: % (SQLSTATE %)', SQLERRM, SQLSTATE;
END;
$$;


--
-- Name: actualizar_usuario(character varying, character varying, character varying, character varying, character varying, text, public.tipo_usuario, character varying, character varying, character varying, character varying, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.actualizar_usuario(IN p_carnet_actual character varying, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_apellido_paterno_nuevo character varying DEFAULT NULL::character varying, IN p_apellido_materno_nuevo character varying DEFAULT NULL::character varying, IN p_email_nuevo character varying DEFAULT NULL::character varying, IN p_contrasena_nueva text DEFAULT NULL::text, IN p_rol_nuevo public.tipo_usuario DEFAULT NULL::public.tipo_usuario, IN p_carrera_nueva character varying DEFAULT NULL::character varying, IN p_telefono_nuevo character varying DEFAULT NULL::character varying, IN p_telefono_ref_nuevo character varying DEFAULT NULL::character varying, IN p_nombre_ref_nuevo character varying DEFAULT NULL::character varying, IN p_email_ref_nuevo character varying DEFAULT NULL::character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_carrera_actual integer;
    v_id_carrera_para_actualizar integer;
BEGIN
    SELECT id_carrera
      INTO v_id_carrera_actual
    FROM public.usuarios
    WHERE carnet = p_carnet_actual
      AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No existe usuario activo con carnet = %', p_carnet_actual;
    END IF;

    IF p_carrera_nueva IS NOT NULL THEN
        SELECT id_carrera
          INTO v_id_carrera_para_actualizar
        FROM public.carreras
        WHERE nombre = p_carrera_nueva
          AND estado_eliminado = FALSE;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'Carrera no encontrada o eliminada: %', p_carrera_nueva;
        END IF;
    ELSE
        v_id_carrera_para_actualizar := v_id_carrera_actual;
    END IF;

    IF p_rol_nuevo IS NOT NULL THEN
        IF p_rol_nuevo NOT IN ('administrador','estudiante') THEN
            RAISE EXCEPTION 'Rol inválido: %. Debe ser administrador o estudiante.', p_rol_nuevo;
        END IF;
    END IF;

    UPDATE public.usuarios
       SET
         nombre            = COALESCE(p_nombre_nuevo, nombre),
         apellido_paterno  = COALESCE(p_apellido_paterno_nuevo, apellido_paterno),
         apellido_materno  = COALESCE(p_apellido_materno_nuevo, apellido_materno),
         email             = COALESCE(p_email_nuevo, email),
         contrasena        = COALESCE(p_contrasena_nueva, contrasena),
         rol               = COALESCE(p_rol_nuevo, rol),
         id_carrera        = v_id_carrera_para_actualizar,
         telefono          = COALESCE(p_telefono_nuevo, telefono),
         telefono_referencia = COALESCE(p_telefono_ref_nuevo, telefono_referencia),
         nombre_referencia = COALESCE(p_nombre_ref_nuevo, nombre_referencia),
         email_referencia  = COALESCE(p_email_ref_nuevo, email_referencia)
     WHERE carnet = p_carnet_actual
       AND estado_eliminado = FALSE;

EXCEPTION
    WHEN unique_violation THEN
        IF SQLERRM LIKE '%usuarios_carnet_key%' THEN
            RAISE EXCEPTION 'Error: El carnet "%" ya está en uso.', p_carnet_actual;
        ELSIF SQLERRM LIKE '%usuarios_email_key%' THEN
            RAISE EXCEPTION 'Error: El email "%" ya está en uso.', p_email_nuevo;
        ELSE
            RAISE EXCEPTION 'Violación de unicidad: %', SQLERRM;
        END IF;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error inesperado al actualizar usuario: % (SQLSTATE %)', SQLERRM, SQLSTATE;
END;
$$;


--
-- Name: eliminar_accesorio(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_accesorio(IN p_id_accesorio integer)
    LANGUAGE plpgsql
    AS $$
BEGIN

    -- 2) Bloquear la fila y verificar existencia de accesorio activo
    PERFORM 1
      FROM public.accesorios
     WHERE id_accesorio    = p_id_accesorio
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un accesorio activo con ID = %.', p_id_accesorio;
    END IF;

    -- 3) Marcar como eliminado lógicamente
    UPDATE public.accesorios
       SET estado_eliminado = TRUE
     WHERE id_accesorio = p_id_accesorio;

EXCEPTION
    WHEN OTHERS THEN
        -- Capturar cualquier error inesperado
        RAISE EXCEPTION 'Error al eliminar lógicamente el accesorio (ID = %): %',
                        p_id_accesorio, SQLERRM;
END;
$$;


--
-- Name: eliminar_carrera(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_carrera(IN p_id_carrera integer)
    LANGUAGE plpgsql
    AS $$
BEGIN

    PERFORM 1
      FROM public.carreras
     WHERE id_carrera      = p_id_carrera
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró una carrera activa con ID = %.', p_id_carrera;
    END IF;

    UPDATE public.carreras
       SET estado_eliminado = TRUE
     WHERE id_carrera = p_id_carrera;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente la carrera (ID = %): %',
                        p_id_carrera, SQLERRM;
END;
$$;


--
-- Name: eliminar_categoria(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_categoria(IN p_id_categoria integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.categorias
     WHERE id_categoria    = p_id_categoria
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró una categoría activa con ID = %.', p_id_categoria;
    END IF;

    UPDATE public.categorias
       SET estado_eliminado = TRUE
     WHERE id_categoria = p_id_categoria;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente la categoría (ID = %): %',
                        p_id_categoria, SQLERRM;
END;
$$;


--
-- Name: eliminar_componente(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_componente(IN p_id_componente integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.componentes
     WHERE id_componente   = p_id_componente
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un componente activo con ID = %.', p_id_componente;
    END IF;

    UPDATE public.componentes
       SET estado_eliminado = TRUE
     WHERE id_componente = p_id_componente;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el componente (ID = %): %',
                        p_id_componente, SQLERRM;
END;
$$;


--
-- Name: eliminar_empresas_mantenimiento(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_empresas_mantenimiento(IN p_id_empresa_mantenimiento integer)
    LANGUAGE plpgsql
    AS $$
BEGIN

    PERFORM 1
      FROM public.empresas_mantenimiento
     WHERE id_empresa_mantenimiento = p_id_empresa_mantenimiento
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un registro activo en empresas_mantenimiento con ID = %.', 
                        p_id_empresa_mantenimiento;
    END IF;

    UPDATE public.empresas_mantenimiento
       SET estado_eliminado = TRUE
     WHERE id_empresa_mantenimiento = p_id_empresa_mantenimiento;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente empresas_mantenimiento (ID = %): %',
                        p_id_empresa_mantenimiento, SQLERRM;
END;
$$;


--
-- Name: eliminar_equipo(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_equipo(IN p_id_equipo integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.equipos
     WHERE id_equipo = p_id_equipo
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un equipo activo con ID = %.', p_id_equipo;
    END IF;

    UPDATE public.equipos
       SET estado_eliminado = TRUE
     WHERE id_equipo = p_id_equipo;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el equipo (ID = %): %',
                        p_id_equipo, SQLERRM;
END;
$$;


--
-- Name: eliminar_gavetero(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_gavetero(IN p_id_gavetero integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.gaveteros
     WHERE id_gavetero = p_id_gavetero
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un gavetero activo con ID = %.', p_id_gavetero;
    END IF;

    UPDATE public.gaveteros
       SET estado_eliminado = TRUE
     WHERE id_gavetero = p_id_gavetero;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el gavetero (ID = %): %',
                        p_id_gavetero, SQLERRM;
END;
$$;


--
-- Name: eliminar_grupo_equipo(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_grupo_equipo(IN p_id_grupo_equipo integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.grupos_equipos
     WHERE id_grupo_equipo = p_id_grupo_equipo
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un grupo de equipos activo con ID = %.', p_id_grupo_equipo;
    END IF;

    UPDATE public.grupos_equipos
       SET estado_eliminado = TRUE
     WHERE id_grupo_equipo = p_id_grupo_equipo;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el grupo de equipos (ID = %): %',
                        p_id_grupo_equipo, SQLERRM;
END;
$$;


--
-- Name: eliminar_mantenimiento(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_mantenimiento(IN p_id_mantenimiento integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.mantenimientos
     WHERE id_mantenimiento = p_id_mantenimiento
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un mantenimiento activo con ID = %.', p_id_mantenimiento;
    END IF;

    UPDATE public.mantenimientos
       SET estado_eliminado = TRUE
     WHERE id_mantenimiento = p_id_mantenimiento;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el mantenimiento (ID = %): %',
                        p_id_mantenimiento, SQLERRM;
END;
$$;


--
-- Name: eliminar_mueble(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_mueble(IN p_id_mueble integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.muebles
     WHERE id_mueble = p_id_mueble
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un mueble activo con ID = %.', p_id_mueble;
    END IF;

    UPDATE public.muebles
       SET estado_eliminado = TRUE
     WHERE id_mueble = p_id_mueble;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el mueble (ID = %): %', 
                        p_id_mueble, SQLERRM;
END;
$$;


--
-- Name: eliminar_prestamo(integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_prestamo(IN p_id_prestamo integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.prestamos
     WHERE id_prestamo     = p_id_prestamo
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un préstamo activo con ID = %.', p_id_prestamo;
    END IF;

    UPDATE public.prestamos
       SET estado_eliminado = TRUE
     WHERE id_prestamo = p_id_prestamo;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el préstamo (ID = %): %', 
                        p_id_prestamo, SQLERRM;
END;
$$;


--
-- Name: eliminar_usuario(character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.eliminar_usuario(IN p_carnet character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM 1
      FROM public.usuarios
     WHERE carnet = p_carnet
       AND estado_eliminado = FALSE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró un usuario activo con carnet = %.', p_carnet;
    END IF;

    UPDATE public.usuarios
       SET estado_eliminado = TRUE
     WHERE carnet = p_carnet;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al eliminar lógicamente el usuario (carnet = %): %', 
                        p_carnet, SQLERRM;
END;
$$;


--
-- Name: fn_actualizar_cantidad_equipo_por_estado(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_actualizar_cantidad_equipo_por_estado() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  IF OLD.estado_eliminado = FALSE AND NEW.estado_eliminado = TRUE THEN
    UPDATE public.grupos_equipos
       SET cantidad = GREATEST(0, COALESCE(cantidad, 0) - 1) 
     WHERE id_grupo_equipo = NEW.id_grupo_equipo;

  ELSIF OLD.estado_eliminado = TRUE AND NEW.estado_eliminado = FALSE THEN
    UPDATE public.grupos_equipos
       SET cantidad = COALESCE(cantidad, 0) + 1
     WHERE id_grupo_equipo = NEW.id_grupo_equipo;
  END IF;

  RETURN NEW;
END;
$$;


--
-- Name: fn_actualizar_cantidad_tras_update_equipos(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_actualizar_cantidad_tras_update_equipos() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Este trigger se activa cuando OLD.id_grupo_equipo es diferente de NEW.id_grupo_equipo.
    -- Solo se realizarán ajustes si el estado_eliminado del equipo no cambia en la misma transacción,
    -- ya que se asume que otro trigger maneja los cambios de cantidad cuando estado_eliminado cambia.

    IF OLD.estado_eliminado = NEW.estado_eliminado THEN
        -- El estado de eliminación no cambió, así que esto es un "movimiento puro".
        IF OLD.estado_eliminado = FALSE THEN
            -- El equipo se movió mientras estaba activo.
            -- Decrementar del grupo antiguo.
            IF OLD.id_grupo_equipo IS NOT NULL THEN
                UPDATE public.grupos_equipos
                SET cantidad = cantidad - 1
                WHERE id_grupo_equipo = OLD.id_grupo_equipo;
            END IF;

            -- Incrementar en el grupo nuevo.
            IF NEW.id_grupo_equipo IS NOT NULL THEN
                UPDATE public.grupos_equipos
                SET cantidad = cantidad + 1
                WHERE id_grupo_equipo = NEW.id_grupo_equipo;
            END IF;
        END IF;
        -- Si el equipo se movió mientras estaba inactivo (OLD.estado_eliminado = TRUE),
        -- no se afectan las cantidades de equipos activos.
    END IF;
    -- Si OLD.estado_eliminado != NEW.estado_eliminado, el trigger que maneja
    -- los cambios de estado_eliminado se encargará de los ajustes de cantidad.

    RETURN NEW;
END;
$$;


--
-- Name: fn_actualizar_conteo_gaveteros_por_estado(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_actualizar_conteo_gaveteros_por_estado() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF OLD.estado_eliminado IS DISTINCT FROM NEW.estado_eliminado THEN

        IF OLD.estado_eliminado = TRUE AND NEW.estado_eliminado = FALSE THEN
            UPDATE public.muebles
               SET numero_gaveteros = COALESCE(numero_gaveteros, 0) + 1
             WHERE id_mueble = NEW.id_mueble;

        ELSIF OLD.estado_eliminado = FALSE AND NEW.estado_eliminado = TRUE THEN
            UPDATE public.muebles
               SET numero_gaveteros = GREATEST(0, COALESCE(numero_gaveteros, 0) - 1)
             WHERE id_mueble = NEW.id_mueble;
        END IF;
    END IF;

    RETURN NEW;
END;
$$;


--
-- Name: fn_actualizar_costo_promedio_grupo(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_actualizar_costo_promedio_grupo() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Cuando se inserta, actualiza o elimina un equipo, recalcula el promedio del grupo
    UPDATE public.grupos_equipos
    SET costo_promedio = (
        SELECT COALESCE(AVG(costo_referencia), 0)
        FROM public.equipos
        WHERE id_grupo_equipo = COALESCE(NEW.id_grupo_equipo, OLD.id_grupo_equipo)
          AND estado_eliminado = FALSE
          AND estado_equipo = 'operativo'
    )
    WHERE id_grupo_equipo = COALESCE(NEW.id_grupo_equipo, OLD.id_grupo_equipo);
    
    RETURN COALESCE(NEW, OLD);
END;
$$;


--
-- Name: fn_actualizar_gavetero_tras_update_mueble(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_actualizar_gavetero_tras_update_mueble() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF OLD.estado_eliminado = NEW.estado_eliminado THEN
        IF OLD.estado_eliminado = FALSE THEN

            IF OLD.id_mueble IS NOT NULL THEN
                UPDATE public.muebles
                SET numero_gaveteros = numero_gaveteros - 1
                WHERE id_mueble = OLD.id_mueble;
            END IF;

            IF NEW.id_mueble IS NOT NULL THEN
                UPDATE public.muebles
                SET numero_gaveteros = numero_gaveteros + 1
                WHERE id_mueble = NEW.id_mueble;
            END IF;
        END IF;
    END IF;

    RETURN NEW;
END;
$$;


--
-- Name: fn_estado_eliminado_mantenimiento_a_detalle(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_estado_eliminado_mantenimiento_a_detalle() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN

    IF OLD.estado_eliminado IS DISTINCT FROM NEW.estado_eliminado THEN
        UPDATE public.detalles_mantenimientos
           SET estado_eliminado = NEW.estado_eliminado 
         WHERE id_mantenimiento = NEW.id_mantenimiento;
    END IF;

    RETURN NEW;
END;
$$;


--
-- Name: fn_estado_eliminado_prestamo_a_detalle(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_estado_eliminado_prestamo_a_detalle() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN

    IF OLD.estado_eliminado IS DISTINCT FROM NEW.estado_eliminado THEN
        UPDATE public.detalles_prestamos
           SET estado_eliminado = NEW.estado_eliminado 
         WHERE id_prestamo = NEW.id_prestamo;
    END IF;


    RETURN NEW;
END;
$$;


--
-- Name: fn_incrementar_cantidad_equipos(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_incrementar_cantidad_equipos() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  -- Aumenta en 1 la cantidad en grupos_equipos
  UPDATE grupos_equipos
     SET cantidad = cantidad + 1
   WHERE id_grupo_equipo = NEW.id_grupo_equipo;
  RETURN NEW;
END;
$$;


--
-- Name: fn_incrementar_numero_gaveteros(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_incrementar_numero_gaveteros() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  UPDATE public.muebles
     SET numero_gaveteros = COALESCE(numero_gaveteros, 0) + 1
   WHERE id_mueble = NEW.id_mueble;

  RETURN NEW;
EXCEPTION
  WHEN OTHERS THEN
    RAISE EXCEPTION 'Error al actualizar numero_gaveteros para mueble %: %',
      NEW.id_mueble, SQLERRM;
END;
$$;


--
-- Name: insertar_accesorios(character varying, character varying, character varying, integer, text, double precision, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_accesorios(IN p_nombre character varying, IN p_modelo character varying, IN p_tipo character varying, IN p_codigo_imt integer, IN p_descripcion text DEFAULT NULL::text, IN p_precio double precision DEFAULT NULL::double precision, IN p_url_data_sheet text DEFAULT NULL::text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_equipo INTEGER;
BEGIN
    SELECT id_equipo
      INTO v_id_equipo
      FROM equipos
     WHERE codigo_imt = p_codigo_imt
       AND estado_eliminado = FALSE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró el equipo con código IMT = %', p_codigo_imt;
    END IF;

    INSERT INTO accesorios(
        nombre,
        descripcion,
        modelo,
        url_data_sheet,
        precio,
        id_equipo,
        tipo,
        estado_eliminado
    )
    VALUES(
        p_nombre,
        p_descripcion,
        p_modelo,
        p_url_data_sheet,
        p_precio,
        v_id_equipo,
        p_tipo,
        FALSE
    );
    
EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe un accesorio con esos datos.';
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar accesorio: %', SQLERRM;
END;
$$;


--
-- Name: insertar_carrera(character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_carrera(IN p_nombre character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF trim(p_nombre) = '' THEN
        RAISE EXCEPTION 'El nombre de la carrera no puede estar vacío';
    END IF;

    -- Intentamos actualizar si existe una carrera eliminada lógicamente
    UPDATE public.carreras
    SET estado_eliminado = FALSE
    WHERE nombre = p_nombre AND estado_eliminado = TRUE;

    -- Si se actualizó alguna fila, terminamos el procedimiento
    IF FOUND THEN
        RETURN;
    END IF;

    -- Verificamos si ya existe una carrera activa con ese nombre
    IF EXISTS (
        SELECT 1 FROM public.carreras
        WHERE nombre = p_nombre AND estado_eliminado = FALSE
    ) THEN
        RAISE EXCEPTION 'Ya existe una carrera con el nombre "%" ', p_nombre;
    END IF;

    -- Insertamos si no existe ningún registro (ni activo ni eliminado)
    INSERT INTO public.carreras (
        nombre,
        estado_eliminado
    )
    VALUES (
        p_nombre,
        FALSE
    );

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar carrera: %', SQLERRM;
END;
$$;


--
-- Name: insertar_categoria(character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_categoria(IN p_nombre_raw character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_nombre TEXT := TRIM(both ' ' FROM p_nombre_raw);
BEGIN
    IF v_nombre = '' THEN
        RAISE EXCEPTION 'El nombre de la categoría no puede estar vacío';
    END IF;

    -- Intentar reactivar si existe una categoría eliminada lógicamente
    UPDATE public.categorias
    SET estado_eliminado = FALSE
    WHERE nombre = v_nombre AND estado_eliminado = TRUE;

    IF FOUND THEN
        RETURN;
    END IF;

    -- Verificar si ya existe una categoría activa con ese nombre
    IF EXISTS (
        SELECT 1
        FROM public.categorias
        WHERE nombre = v_nombre AND estado_eliminado = FALSE
    ) THEN
        RAISE EXCEPTION 'Ya existe una categoría con el nombre "%"', v_nombre;
    END IF;

    -- Insertar si no existe
    INSERT INTO public.categorias (
        nombre,
        estado_eliminado
    )
    VALUES (
        v_nombre,
        FALSE
    );

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe una categoría con el nombre "%"', v_nombre;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar categoría: %', SQLERRM;
END;
$$;


--
-- Name: insertar_componente(character varying, character varying, character varying, integer, text, double precision, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_componente(IN p_nombre character varying, IN p_modelo character varying, IN p_tipo character varying, IN p_codigo_imt integer, IN p_descripcion text DEFAULT NULL::text, IN p_precio_referencia double precision DEFAULT NULL::double precision, IN p_url_data_sheet text DEFAULT NULL::text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_equipo INTEGER;
BEGIN
    SELECT id_equipo
      INTO v_id_equipo
      FROM equipos
     WHERE codigo_imt      = p_codigo_imt
       AND estado_eliminado = FALSE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró el equipo con código IMT = %', p_codigo_imt;
    END IF;

    INSERT INTO componentes (
        nombre,
        modelo,
        tipo,
        descripcion,
        url_data_sheet,
        precio_referencia,
        id_equipo,
        estado_eliminado
    )
    VALUES (
        p_nombre,
        p_modelo,
        p_tipo,
        p_descripcion,
        p_url_data_sheet,
        p_precio_referencia,
        v_id_equipo,
        FALSE
    );

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe un componente con esos datos.';
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar componente: %', SQLERRM;
END;
$$;


--
-- Name: insertar_empresa_mantenimiento(character varying, character varying, character varying, character varying, text, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_empresa_mantenimiento(IN p_nombre character varying, IN p_nombre_responsable character varying, IN p_apellido_responsable character varying, IN p_telefono character varying, IN p_direccion text, IN p_nit character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_nombre_trimmed TEXT := TRIM(both ' ' FROM p_nombre);
BEGIN
    -- 1) Reactivar si ya había una empresa con ese nombre eliminada lógicamente
    UPDATE public.empresas_mantenimiento
       SET estado_eliminado = FALSE
     WHERE nombre = v_nombre_trimmed
       AND estado_eliminado = TRUE;
    IF FOUND THEN
        RETURN;
    END IF;

    -- 2) Verificar si ya existe una empresa activa con ese nombre
    IF EXISTS (
        SELECT 1
          FROM public.empresas_mantenimiento
         WHERE nombre = v_nombre_trimmed
           AND estado_eliminado = FALSE
    ) THEN
        RAISE EXCEPTION 'Ya existe una empresa de mantenimiento con esos datos.';
    END IF;

    -- 3) Insertar nueva empresa
    INSERT INTO public.empresas_mantenimiento (
        nombre,
        nombre_responsable,
        apellido_responsable,
        telefono,
        direccion,
        nit,
        estado_eliminado
    )
    VALUES (
        v_nombre_trimmed,
        p_nombre_responsable,
        p_apellido_responsable,
        p_telefono,
        p_direccion,
        p_nit,
        FALSE
    );

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe una empresa de mantenimiento con esos datos.';
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar empresa de mantenimiento: %', SQLERRM;
END;
$$;


--
-- Name: insertar_equipo(character varying, character varying, character varying, character varying, text, character varying, character varying, character varying, double precision, integer, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_equipo(IN p_nombre_grupo_equipo character varying, IN p_modelo character varying, IN p_marca character varying, IN p_codigo_ucb character varying, IN p_descripcion text, IN p_numero_serial character varying, IN p_ubicacion character varying, IN p_procedencia character varying, IN p_costo_referencia double precision, IN p_tiempo_maximo_prestamo integer, IN p_nombre_gavetero character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_grupo_equipo INTEGER;
    v_id_gavetero     INTEGER;
    v_codigo_imt      INTEGER;
BEGIN
    SELECT ge.id_grupo_equipo
      INTO v_id_grupo_equipo
      FROM grupos_equipos AS ge
     WHERE ge.nombre           = p_nombre_grupo_equipo
	 	and ge.modelo			=p_modelo
		 and ge.marca 			=p_marca
       AND ge.estado_eliminado = FALSE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró el grupo de equipos con nombre = %', p_nombre_grupo_equipo;
    END IF;
	
    IF p_nombre_gavetero IS NOT NULL THEN
    	SELECT g.id_gavetero
      INTO v_id_gavetero
      FROM gaveteros AS g
     WHERE g.nombre = p_nombre_gavetero
       AND g.estado_eliminado = FALSE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontro el gavetero con nombre = %', p_nombre_gavetero;
    END IF;
	ELSE
    v_id_gavetero := NULL;
	END IF;

    v_codigo_imt := public.obtener_codigo_imt(v_id_grupo_equipo);

    INSERT INTO equipos (
        codigo_imt,
        codigo_ucb,
        descripcion,
        numero_serial,
        ubicacion,
        procedencia,
        costo_referencia,
        tiempo_max_prestamo,
        id_gavetero,
        id_grupo_equipo,
        estado_eliminado
    )
    VALUES (
        v_codigo_imt,
        p_codigo_ucb,
        p_descripcion,
        p_numero_serial,
        p_ubicacion,
        p_procedencia,
        p_costo_referencia,
        p_tiempo_maximo_prestamo,
        v_id_gavetero,
        v_id_grupo_equipo,
        FALSE
    );

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe un equipo con ese código UCB o número serial.';
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar equipo: %', SQLERRM;
END;
$$;


--
-- Name: insertar_gavetero(character varying, character varying, character varying, double precision, double precision, double precision); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_gavetero(IN p_nombre character varying, IN p_tipo character varying, IN p_nombre_mueble character varying, IN p_longitud double precision, IN p_profundidad double precision, IN p_altura double precision)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_mueble INTEGER;
BEGIN
    SELECT m.id_mueble
      INTO v_id_mueble
      FROM muebles AS m
     WHERE m.nombre           = p_nombre_mueble
       AND m.estado_eliminado = FALSE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró el mueble con nombre = %', p_nombre_mueble;
    END IF;

    IF EXISTS (
        SELECT 1
          FROM gaveteros
         WHERE nombre = p_nombre
           AND estado_eliminado = FALSE
    ) THEN
        RAISE EXCEPTION 'Ya existe un gavetero con nombre = %', p_nombre;
    END IF;

    INSERT INTO gaveteros (
        nombre,
        tipo,
        id_mueble,
        longitud,
        profundidad,
        altura,
        estado_eliminado
    )
    VALUES (
        p_nombre,
        p_tipo,
        v_id_mueble,
        p_longitud,
        p_profundidad,
        p_altura,
        FALSE
    );

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Violación de unicidad al intentar insertar gavetero: %', p_nombre;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar gavetero: %', SQLERRM;
END;
$$;


--
-- Name: insertar_grupo_equipo(character varying, character varying, character varying, text, character varying, text, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_grupo_equipo(IN p_nombre character varying, IN p_modelo character varying, IN p_marca character varying, IN p_descripcion text, IN p_nombre_categoria character varying, IN p_url_data_sheet text, IN p_url_imagen text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_categoria INTEGER;
BEGIN
    SELECT c.id_categoria
      INTO v_id_categoria
      FROM categorias AS c
     WHERE c.nombre           = p_nombre_categoria
       AND c.estado_eliminado = FALSE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró la categoría con nombre = %', p_nombre_categoria;
    END IF;

    IF EXISTS (
        SELECT 1
          FROM grupos_equipos AS ge
         WHERE ge.nombre            = p_nombre
           AND ge.modelo            = p_modelo
           AND ge.marca             = p_marca
           AND ge.estado_eliminado  = FALSE
    ) THEN
        RAISE EXCEPTION 'Ya existe un grupo de equipos con nombre = %, modelo = %, marca = %',
            p_nombre, p_modelo, p_marca;
    END IF;

    INSERT INTO grupos_equipos (
        nombre,
        modelo,
        marca,
        descripcion,
        id_categoria,
        url_data_sheet,
        url_imagen,
        estado_eliminado
    )
    VALUES (
        p_nombre,
        p_modelo,
        p_marca,
        p_descripcion,
        v_id_categoria,
        p_url_data_sheet,
        p_url_imagen,
        FALSE
    );

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Violación de unicidad al intentar insertar grupo de equipos: % / % / %',
            p_nombre, p_modelo, p_marca;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar grupo de equipos: %', SQLERRM;
END;
$$;


--
-- Name: insertar_mantenimiento(date, date, character varying, double precision, text, integer[], character varying[], text[]); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_mantenimiento(IN p_fecha_mantenimiento date, IN p_fecha_final_mantenimiento date, IN p_nombre_empresa character varying, IN p_costo double precision, IN p_descripcion text, IN p_codigos_imt integer[], IN p_tipos_mantenimiento character varying[], IN p_descripciones_equipo text[])
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_mantenimiento         INTEGER;
    v_id_empresa_mantenimiento INTEGER;
    v_id_equipo                INTEGER;
    i                          INTEGER;
BEGIN
    SELECT em.id_empresa_mantenimiento
      INTO v_id_empresa_mantenimiento
      FROM empresas_mantenimiento AS em
     WHERE em.nombre           = p_nombre_empresa
       AND em.estado_eliminado = FALSE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró la empresa con nombre = %', p_nombre_empresa;
    END IF;

    INSERT INTO mantenimientos (
        fecha_mantenimiento,
        fecha_final_mantenimiento,
        id_empresa,
        costo,
        descripcion,
        estado_eliminado
    )
    VALUES (
        p_fecha_mantenimiento,
        p_fecha_final_mantenimiento,
        v_id_empresa_mantenimiento,
        p_costo,
        p_descripcion,
        FALSE
    )
    RETURNING id_mantenimiento
    INTO v_id_mantenimiento;

    IF array_length(p_codigos_imt, 1) IS NULL
       OR array_length(p_codigos_imt, 1) <> array_length(p_tipos_mantenimiento, 1)
       OR array_length(p_codigos_imt, 1) <> array_length(p_descripciones_equipo, 1)
    THEN
        RAISE EXCEPTION 'Los arreglos de equipos deben tener la misma longitud';
    END IF;

    FOR i IN 1..array_length(p_codigos_imt, 1) LOOP
        SELECT e.id_equipo
          INTO v_id_equipo
          FROM equipos AS e
         WHERE e.codigo_imt       = p_codigos_imt[i]
           AND e.estado_eliminado = FALSE;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'Equipo no encontrado con código IMT = %', p_codigos_imt[i];
        END IF;

        INSERT INTO detalles_mantenimientos (
            id_mantenimiento,
            id_equipo,
            tipo_mantenimiento,
            descripcion,
            estado_eliminado
        )
        VALUES (
            v_id_mantenimiento,
            v_id_equipo,
            p_tipos_mantenimiento[i],  
            p_descripciones_equipo[i],
            FALSE
        );
    END LOOP;

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Violación de unicidad al insertar mantenimiento';
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar mantenimiento: %', SQLERRM;
END;
$$;


--
-- Name: insertar_mueble(character varying, character varying, double precision, character varying, double precision, double precision, double precision); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_mueble(IN p_nombre character varying, IN p_tipo character varying, IN p_costo double precision, IN p_ubicacion character varying, IN p_longitud double precision, IN p_profundidad double precision, IN p_altura double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO muebles (
        nombre,
        tipo,
        costo,
        ubicacion,
        longitud,
        profundidad,
        altura,
        estado_eliminado
    )
    VALUES (
        p_nombre,
        p_tipo,
        p_costo,
        p_ubicacion,
        p_longitud,
        p_profundidad,
        p_altura,
        FALSE
    );
EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Ya existe un mueble con el mismo nombre.';
   	WHEN OTHERS THEN
        RAISE EXCEPTION 'Error al insertar mueble: %', SQLERRM;
END;
$$;


--
-- Name: insertar_prestamo(integer[], timestamp without time zone, timestamp without time zone, text, character varying, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_prestamo(IN id_grupos_equipo_input integer[], IN fecha_prestamo_esperada_input timestamp without time zone, IN fecha_devolucion_esperada_input timestamp without time zone, IN observacion_input text, IN carnet_input character varying, IN id_contrato_input text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    id_prestamo_nuevo integer;
    id_grupo integer;
    id_equipo_disponible integer;
    equipos_ya_asignados_a_este_prestamo integer[] := ARRAY[]::integer[];
    contador integer := 0;
BEGIN
    -- Primera pasada: verificar disponibilidad para todos los grupos
    FOREACH id_grupo IN ARRAY id_grupos_equipo_input LOOP
        SELECT COUNT(e.id_equipo) INTO contador
        FROM public.equipos e
        WHERE e.id_grupo_equipo = id_grupo
          AND e.estado_eliminado = FALSE
          AND e.estado_equipo = 'operativo'
          AND NOT EXISTS (
              SELECT 1 FROM public.detalles_prestamos dp
              INNER JOIN public.prestamos p ON dp.id_prestamo = p.id_prestamo
              WHERE dp.id_equipo = e.id_equipo
                AND p.estado_eliminado = FALSE
                AND p.estado_prestamo IN ('pendiente', 'aprobado', 'activo')
                AND fecha_prestamo_esperada_input::date BETWEEN p.fecha_prestamo_esperada::date AND p.fecha_devolucion_esperada::date
          )
          AND NOT EXISTS (
              SELECT 1 FROM public.detalles_mantenimientos dm
              INNER JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
              WHERE dm.id_equipo = e.id_equipo
                AND m.estado_eliminado = FALSE
                AND fecha_prestamo_esperada_input::date BETWEEN m.fecha_mantenimiento AND m.fecha_final_mantenimiento
          );

        IF contador = 0 THEN
            RAISE EXCEPTION 'No hay equipos disponibles para el grupo %', id_grupo;
        END IF;
    END LOOP;

    -- Insertar el préstamo principal
    INSERT INTO public.prestamos (
        carnet,
        fecha_solicitud,
        fecha_prestamo_esperada,
        fecha_devolucion_esperada,
        observacion,
        estado_prestamo,
        id_contrato,
        estado_eliminado
    )
    VALUES (
        carnet_input,
        (now() AT TIME ZONE 'America/La_Paz')::timestamp without time zone,
        fecha_prestamo_esperada_input,
        fecha_devolucion_esperada_input,
        observacion_input,
        'pendiente'::estado_prestamo,
        id_contrato_input,
        FALSE
    )
    RETURNING id_prestamo INTO id_prestamo_nuevo;

    -- Segunda pasada: asignar equipos específicos
    FOREACH id_grupo IN ARRAY id_grupos_equipo_input LOOP
        SELECT e.id_equipo INTO id_equipo_disponible
        FROM public.equipos e
        WHERE e.id_grupo_equipo = id_grupo
          AND e.estado_eliminado = FALSE
          AND e.estado_equipo = 'operativo'
          AND NOT (e.id_equipo = ANY(equipos_ya_asignados_a_este_prestamo))
          AND NOT EXISTS (
              SELECT 1 FROM public.detalles_prestamos dp
              INNER JOIN public.prestamos p ON dp.id_prestamo = p.id_prestamo
              WHERE dp.id_equipo = e.id_equipo
                AND p.estado_eliminado = FALSE
                AND p.estado_prestamo IN ('pendiente', 'aprobado', 'activo')
                AND fecha_prestamo_esperada_input::date BETWEEN p.fecha_prestamo_esperada::date AND p.fecha_devolucion_esperada::date
          )
          AND NOT EXISTS (
              SELECT 1 FROM public.detalles_mantenimientos dm
              INNER JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
              WHERE dm.id_equipo = e.id_equipo
                AND m.estado_eliminado = FALSE
                AND fecha_prestamo_esperada_input::date BETWEEN m.fecha_mantenimiento AND m.fecha_final_mantenimiento
          )
        LIMIT 1;

        IF id_equipo_disponible IS NOT NULL THEN
            INSERT INTO public.detalles_prestamos (id_prestamo, id_equipo, estado_eliminado)
            VALUES (id_prestamo_nuevo, id_equipo_disponible, FALSE);

            equipos_ya_asignados_a_este_prestamo := array_append(equipos_ya_asignados_a_este_prestamo, id_equipo_disponible);
        END IF;
    END LOOP;
END;
$$;


--
-- Name: insertar_prestamo(integer[], timestamp with time zone, timestamp with time zone, text, character varying, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_prestamo(IN id_grupos_equipo_input integer[], IN fecha_prestamo_esperada_input timestamp with time zone, IN fecha_devolucion_esperada_input timestamp with time zone, IN observacion_input text, IN carnet_input character varying, IN id_contrato_input text)
    LANGUAGE plpgsql
    AS $$
DECLARE
    id_grupo integer;
    id_equipo_disponible integer;
    id_prestamo_general integer;  
    equipos_ya_asignados_a_este_prestamo integer[] := ARRAY[]::integer[];
    equipo_tmp integer;
    dias_solicitados integer;
    tiempo_maximo_permitido integer;
BEGIN
    -- Calcular días solicitados
    dias_solicitados := (fecha_devolucion_esperada_input::date - fecha_prestamo_esperada_input::date);
    
    -- PRIMERA PASADA: Verificación de disponibilidad para todos los grupos
    FOREACH id_grupo IN ARRAY id_grupos_equipo_input LOOP
        IF NOT EXISTS(
            SELECT 1 
              FROM public.grupos_equipos ge 
             WHERE ge.id_grupo_equipo = id_grupo
               AND ge.estado_eliminado = FALSE
        ) THEN
            RAISE EXCEPTION 'Grupo ID % no existe o está eliminado. No se puede continuar con la asignación de equipos para este grupo.', id_grupo;
        END IF;

        SELECT e.id_equipo
          INTO equipo_tmp
          FROM public.equipos e
         WHERE e.id_grupo_equipo = id_grupo
           AND e.estado_eliminado = FALSE
           AND e.estado_equipo = 'operativo'
           AND dias_solicitados <= e.tiempo_max_prestamo
           AND NOT EXISTS ( 
               SELECT 1
                 FROM public.detalles_prestamos dp
                 JOIN public.prestamos pr ON dp.id_prestamo = pr.id_prestamo
                WHERE dp.id_equipo = e.id_equipo
                  AND pr.estado_eliminado = FALSE
                  AND pr.estado_prestamo IN ('pendiente', 'aprobado', 'activo')
                  AND (
                      (fecha_prestamo_esperada_input < pr.fecha_devolucion_esperada AND fecha_devolucion_esperada_input > pr.fecha_prestamo_esperada)
                  )
           )
           AND NOT EXISTS ( 
               SELECT 1
                 FROM public.detalles_mantenimientos dm
                 JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
                WHERE dm.id_equipo = e.id_equipo
                  AND m.estado_eliminado = FALSE
                  AND (
                      (fecha_prestamo_esperada_input < m.fecha_final_mantenimiento AND fecha_devolucion_esperada_input > m.fecha_mantenimiento)
                  )
           )
         ORDER BY e.id_equipo 
         LIMIT 1;

        IF equipo_tmp IS NULL THEN
            RAISE EXCEPTION 'No se encontró equipo disponible para el grupo ID % en las fechas solicitadas (o los equipos exceden el tiempo máximo de préstamo permitido).', id_grupo;
        END IF;
    END LOOP;

    -- SEGUNDA PASADA: Inserción del préstamo y detalles
    BEGIN
        INSERT INTO public.prestamos (
            fecha_prestamo_esperada,
            fecha_devolucion_esperada,
            observacion,
            carnet,
            id_contrato
        ) VALUES (
            fecha_prestamo_esperada_input,
            fecha_devolucion_esperada_input,
            observacion_input,
            carnet_input,
            id_contrato_input
        )
        RETURNING id_prestamo INTO id_prestamo_general;

    EXCEPTION
        WHEN unique_violation THEN
            RAISE EXCEPTION 'Error al crear préstamo general: Conflicto de llave única. %', SQLERRM;
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error inesperado (%s) al crear el préstamo general: %', SQLSTATE, SQLERRM;
    END;

    IF id_prestamo_general IS NULL THEN
        RAISE EXCEPTION 'Fallo crítico: No se pudo obtener el ID del préstamo general creado.';
    END IF;

    FOREACH id_grupo IN ARRAY id_grupos_equipo_input LOOP
        id_equipo_disponible := NULL; 

        SELECT e.id_equipo
          INTO id_equipo_disponible
          FROM public.equipos e
         WHERE e.id_grupo_equipo = id_grupo
           AND e.estado_eliminado = FALSE
           AND e.estado_equipo = 'operativo'
           AND dias_solicitados <= e.tiempo_max_prestamo
           AND (equipos_ya_asignados_a_este_prestamo IS NULL OR NOT (e.id_equipo = ANY(equipos_ya_asignados_a_este_prestamo)))
           AND NOT EXISTS ( 
               SELECT 1
                 FROM public.detalles_prestamos dp
                 JOIN public.prestamos pr ON dp.id_prestamo = pr.id_prestamo
                WHERE dp.id_equipo = e.id_equipo
                  AND pr.id_prestamo <> id_prestamo_general 
                  AND pr.estado_eliminado = FALSE
                  AND pr.estado_prestamo IN ('pendiente', 'aprobado', 'activo')
                  AND (
                      (fecha_prestamo_esperada_input < pr.fecha_devolucion_esperada AND fecha_devolucion_esperada_input > pr.fecha_prestamo_esperada)
                  )
           )
           AND NOT EXISTS ( 
               SELECT 1
                 FROM public.detalles_mantenimientos dm
                 JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
                WHERE dm.id_equipo = e.id_equipo
                  AND m.estado_eliminado = FALSE
                  AND (
                      (fecha_prestamo_esperada_input < m.fecha_final_mantenimiento AND fecha_devolucion_esperada_input > m.fecha_mantenimiento)
                  )
           )
         ORDER BY e.id_equipo 
         LIMIT 1;

        IF id_equipo_disponible IS NULL THEN
            RAISE EXCEPTION 'No se encontró equipo disponible para el grupo ID % en las fechas solicitadas (o los equipos exceden el tiempo máximo de préstamo permitido).', id_grupo;
        END IF;

        BEGIN
            INSERT INTO public.detalles_prestamos (
                id_prestamo,
                id_equipo
            ) VALUES (
                id_prestamo_general, 
                id_equipo_disponible
            );

             equipos_ya_asignados_a_este_prestamo := array_append(equipos_ya_asignados_a_este_prestamo, id_equipo_disponible);

        EXCEPTION
            WHEN unique_violation THEN
                RAISE EXCEPTION 'Conflicto de llave única al crear detalle para PréstamoID %, GrupoID %, EquipoID %: %. Esto podría indicar un intento de asignar el mismo equipo dos veces si la tabla detalles_prestamos tiene una restricción de unicidad en (id_prestamo, id_equipo).',
                    id_prestamo_general, id_grupo, id_equipo_disponible, SQLERRM;
            WHEN check_violation THEN
                RAISE EXCEPTION 'Violación de restricción CHECK al crear detalle para PréstamoID %, GrupoID %, EquipoID %: %.',
                    id_prestamo_general, id_grupo, id_equipo_disponible, SQLERRM;
            WHEN foreign_key_violation THEN
                RAISE EXCEPTION 'Violación de llave foránea al crear detalle para PréstamoID %, GrupoID %, EquipoID %: %.',
                    id_prestamo_general, id_grupo, id_equipo_disponible, SQLERRM;
            WHEN OTHERS THEN
                RAISE EXCEPTION 'Error inesperado (%s) al crear detalle para PréstamoID %, GrupoID %, EquipoID %: %.',
                    SQLSTATE, id_prestamo_general, id_grupo, id_equipo_disponible, SQLERRM;
        END; 
    END LOOP; 
END;
$$;


--
-- Name: insertar_usuario(character varying, character varying, character varying, character varying, public.tipo_usuario, character varying, text, character varying, character varying, character varying, character varying, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.insertar_usuario(IN carnet_input character varying, IN nombre_input character varying, IN apellido_paterno_input character varying, IN apellido_materno_input character varying, IN rol_input public.tipo_usuario, IN email_input character varying, IN contrasena_input text, IN carrera_input character varying, IN telefono_input character varying, IN telefono_referencia_input character varying, IN nombre_referencia_input character varying, IN email_referencia_input character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DECLARE
        id_carrera_input INTEGER;
    BEGIN
        SELECT
            id_carrera INTO id_carrera_input
        FROM
            carreras as c
        WHERE
            nombre = carrera_input and
			c.estado_eliminado=false;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'No se encontró la carrera con nombre: %',
            carrera_input;
        END IF;
        INSERT INTO usuarios (
            carnet,
            nombre,
            apellido_paterno,
            apellido_materno,
            rol,
            email,
            contrasena,
            id_carrera,
            telefono,
            telefono_referencia,
            nombre_referencia,
            email_referencia,
            estado_eliminado
        )
        VALUES
            (
                carnet_input,
                nombre_input,
                apellido_paterno_input,
                apellido_materno_input,
                rol_input,
                email_input,
                contrasena_input,
                id_carrera_input,
                telefono_input,
                telefono_referencia_input,
                nombre_referencia_input,
                email_referencia_input,
                FALSE
            );
    EXCEPTION
        WHEN UNIQUE_VIOLATION THEN
            RAISE EXCEPTION 'Error: El carnet o email ya está registrado en la base de datos.';
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Hubo un error inesperado durante el proceso de inserción: %',
            SQLERRM;
    END;
END;
$$;


--
-- Name: insertar_y_obtener_prestamo(integer[], timestamp without time zone, timestamp without time zone, text, character varying, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insertar_y_obtener_prestamo(id_grupos_equipo_input integer[], fecha_prestamo_esperada_input timestamp without time zone, fecha_devolucion_esperada_input timestamp without time zone, observacion_input text, carnet_input character varying, id_contrato_input text) RETURNS TABLE(id_prestamo integer, id_equipo integer, codigo_imt character varying, codigo_serial character varying, nombre character varying, modelo character varying, marca character varying, id_grupo_equipo integer)
    LANGUAGE plpgsql
    AS $$
DECLARE
    id_grupo_actual integer;
    id_equipo_disponible integer;
    id_prestamo_general integer;
    dias_solicitados integer;
    equipos_asignados integer[] := ARRAY[]::integer[];
    fecha_prestamo_ajustada timestamp without time zone;
    fecha_devolucion_ajustada timestamp without time zone;
BEGIN
    -- VALIDACIONES INICIALES
    -- Validar que el usuario existe
    IF NOT EXISTS(SELECT 1 FROM public.usuarios WHERE carnet = carnet_input AND estado_eliminado = FALSE) THEN
        RAISE EXCEPTION 'Usuario con carnet % no existe o está eliminado.', carnet_input;
    END IF;

    -- Las fechas se usan tal cual vienen del frontend
    -- Si fecha_inicio = fecha_fin → 1 día
    -- Si fecha_fin = fecha_inicio + 1 → 2 días
    -- etc.
    fecha_prestamo_ajustada := fecha_prestamo_esperada_input;
    fecha_devolucion_ajustada := fecha_devolucion_esperada_input;

    -- Validar que la fecha de devolución no sea anterior a la de préstamo
    IF fecha_devolucion_ajustada::date < fecha_prestamo_ajustada::date THEN
        RAISE EXCEPTION 'La fecha de devolución no puede ser anterior a la fecha de préstamo.';
    END IF;

    -- Calcular días solicitados (diferencia + 1 = días totales ocupados)
    dias_solicitados := (fecha_devolucion_ajustada::date - fecha_prestamo_ajustada::date) + 1;

    -- REMOVIDO: Verificación de solapamiento por grupo (causaba rechazos innecesarios)
    -- Ahora confiamos en la búsqueda de equipos libres para manejar conflictos

    -- Crear el préstamo
    INSERT INTO public.prestamos (
        fecha_prestamo_esperada,
        fecha_devolucion_esperada,
        observacion,
        carnet,
        id_contrato
    ) VALUES (
        fecha_prestamo_ajustada,
        fecha_devolucion_ajustada,
        observacion_input,
        carnet_input,
        id_contrato_input
    )
    RETURNING prestamos.id_prestamo INTO id_prestamo_general;

    -- Para cada grupo, encontrar y asignar un equipo disponible
    FOREACH id_grupo_actual IN ARRAY id_grupos_equipo_input LOOP
        -- Validar que el grupo existe (mover aquí, después de crear préstamo)
        IF NOT EXISTS(SELECT 1 FROM public.grupos_equipos ge WHERE ge.id_grupo_equipo = id_grupo_actual AND ge.estado_eliminado = FALSE) THEN
            -- Eliminar el préstamo creado si falla
            DELETE FROM public.prestamos WHERE prestamos.id_prestamo = id_prestamo_general;
            RAISE EXCEPTION 'Grupo de equipo ID % no existe o está eliminado.', id_grupo_actual;
        END IF;

        -- Buscar equipo disponible para este grupo
        SELECT e.id_equipo
        INTO id_equipo_disponible
        FROM public.equipos e
        WHERE e.id_grupo_equipo = id_grupo_actual
          AND e.estado_eliminado = FALSE
          AND e.estado_equipo = 'operativo'
          AND dias_solicitados <= COALESCE(e.tiempo_max_prestamo, 9999)
          -- Excluir equipos ya asignados en este préstamo
          AND NOT (e.id_equipo = ANY(equipos_asignados))
          -- Excluir equipos ocupados en otros préstamos (verificación diaria)
          AND NOT EXISTS (
              SELECT 1
              FROM generate_series(fecha_prestamo_ajustada::date, fecha_devolucion_ajustada::date, INTERVAL '1 day') AS gs(fecha_dia)
              WHERE EXISTS (
                  SELECT 1
                  FROM public.detalles_prestamos dp
                  INNER JOIN public.prestamos pr ON dp.id_prestamo = pr.id_prestamo
                  WHERE dp.id_equipo = e.id_equipo
                    AND pr.estado_eliminado = FALSE
                    AND pr.estado_prestamo IN ('pendiente', 'aprobado', 'activo')
                    AND gs.fecha_dia BETWEEN pr.fecha_prestamo_esperada::date AND pr.fecha_devolucion_esperada::date
              )
          )
          -- Excluir equipos en mantenimiento
          AND NOT EXISTS (
              SELECT 1
              FROM public.detalles_mantenimientos dm
              JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
              WHERE dm.id_equipo = e.id_equipo
                AND m.estado_eliminado = FALSE
                AND (fecha_prestamo_ajustada::date <= m.fecha_final_mantenimiento
                     AND fecha_devolucion_ajustada::date >= m.fecha_mantenimiento)
          )
        ORDER BY e.id_equipo
        LIMIT 1;

        -- Si no hay equipo disponible, hacer rollback y error
        IF id_equipo_disponible IS NULL THEN
            -- Eliminar el préstamo creado (cascade eliminará detalles)
            DELETE FROM public.prestamos WHERE prestamos.id_prestamo = id_prestamo_general;
            RAISE EXCEPTION 'No se encontró equipo disponible para el grupo ID % en las fechas solicitadas.', id_grupo_actual;
        END IF;

        -- Asignar el equipo al préstamo
        INSERT INTO public.detalles_prestamos (id_prestamo, id_equipo)
        VALUES (id_prestamo_general, id_equipo_disponible);

        -- Agregar a la lista de equipos asignados
        equipos_asignados := array_append(equipos_asignados, id_equipo_disponible);
    END LOOP;

    -- RETORNAR los equipos asignados con sus datos
    RETURN QUERY
    SELECT
        dp.id_prestamo,
        e.id_equipo,
        e.codigo_imt::character varying,
        e.numero_serial,
        ge.nombre,
        ge.modelo,
        ge.marca,
        e.id_grupo_equipo
    FROM public.detalles_prestamos dp
    INNER JOIN public.equipos e ON dp.id_equipo = e.id_equipo
    INNER JOIN public.grupos_equipos ge ON ge.id_grupo_equipo = e.id_grupo_equipo
    WHERE dp.id_prestamo = id_prestamo_general
    ORDER BY e.id_grupo_equipo, e.id_equipo;
END;
$$;


--
-- Name: obtener_accesorios(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_accesorios() RETURNS TABLE(id_accesorio integer, nombre_accesorio character varying, modelo_accesorio character varying, tipo_accesorio character varying, precio_accesorio double precision, nombre_equipo_asociado character varying, codigo_imt_equipo_asociado integer, descripcion_accesorio text, url_data_sheet_accesorio text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        a.id_accesorio,
        a.nombre AS nombre_accesorio,
        a.modelo AS modelo_accesorio,
        a.tipo AS tipo_accesorio,
        a.precio AS precio_accesorio,
        ge.nombre AS nombre_equipo_asociado,   
        e.codigo_imt AS codigo_imt_equipo_asociado, 
        a.descripcion AS descripcion_accesorio,
		a.url_data_sheet as url_data_sheet_accesorio
    FROM
        public.accesorios AS a
    LEFT JOIN 
        public.equipos AS e ON e.id_equipo = a.id_equipo
                           AND e.estado_eliminado = FALSE
    LEFT JOIN
        public.grupos_equipos AS ge ON ge.id_grupo_equipo = e.id_grupo_equipo
                                  AND ge.estado_eliminado = FALSE
    WHERE
        a.estado_eliminado = FALSE; 
END;
$$;


--
-- Name: obtener_carreras(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_carreras() RETURNS TABLE(id_carrera integer, nombre_carrera character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		c.id_carrera,
        c.nombre -- Referencing the 'nombre' column from the 'carreras' table
    FROM
        public.carreras AS c -- Aliasing the table for clarity, though not strictly necessary for a single table
    WHERE
        c.estado_eliminado = FALSE;
END;
$$;


--
-- Name: obtener_categorias(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_categorias() RETURNS TABLE(id_categoria integer, categoria character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
	SELECT 
	c.id_categoria,
	c.nombre
	from categorias as c
	where c.estado_eliminado=false;
END;
$$;


--
-- Name: obtener_codigo_imt(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_codigo_imt(p_id_grupo_equipo integer) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_categoria  integer;
    v_max_sufijo    integer;
    v_codigo_imt    integer;
BEGIN
    -- 1) Obtener la categoría del grupo
    SELECT ge.id_categoria
      INTO v_id_categoria
      FROM grupos_equipos AS ge
     WHERE ge.id_grupo_equipo = p_id_grupo_equipo;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No se encontró el grupo de equipos con id = %', p_id_grupo_equipo;
    END IF;

    -- 2) Calcular el mayor sufijo ya usado para esa categoría
    SELECT COALESCE(MAX(e.codigo_imt % 10000000), 0)
      INTO v_max_sufijo
      FROM grupos_equipos AS ge
      JOIN equipos          AS e
        ON e.id_grupo_equipo = ge.id_grupo_equipo
     WHERE ge.id_categoria = v_id_categoria;

    -- 3) Generar el siguiente código IMT
    v_codigo_imt := v_id_categoria * 10000000 + (v_max_sufijo + 1);

    RETURN v_codigo_imt;
END;
$$;


--
-- Name: obtener_componentes(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_componentes() RETURNS TABLE(id_componente integer, nombre_componente character varying, modelo_componente character varying, tipo_componente character varying, descripcion_componente text, precio_referencia_componente double precision, nombre_equipo character varying, codigo_imt_equipo integer, url_data_sheet_equipo text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		c.id_componente,
        c.nombre,
        c.modelo,
        c.tipo,
        c.descripcion,
        c.precio_referencia,
        ge.nombre AS nombre_equipo, 
        e.codigo_imt,
		c.url_data_sheet
    FROM
        public.componentes AS c
    left JOIN
        public.equipos AS e 
		ON c.id_equipo = e.id_equipo
		and e.estado_eliminado=false
    left JOIN
        public.grupos_equipos AS ge 
		ON ge.id_grupo_equipo = e.id_grupo_equipo
		and ge.estado_eliminado=false
    WHERE
        c.estado_eliminado = FALSE; 
                                  
END;
$$;


--
-- Name: obtener_disponibilidad_equipos_por_fechas_y_id_grupos_equipos(timestamp without time zone, timestamp without time zone, integer[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_disponibilidad_equipos_por_fechas_y_id_grupos_equipos(fecha_inicio timestamp without time zone, fecha_fin timestamp without time zone, p_array_ids integer[]) RETURNS TABLE(fecha timestamp without time zone, id_grupo_equipo integer, cantidad_disponible bigint)
    LANGUAGE plpgsql
    AS $$
DECLARE
    fecha_actual date;
    grupo_id integer;
    disponibilidad bigint;
    dias_solicitados integer;
BEGIN
    -- Calcular días solicitados (para verificación de tiempo_max_prestamo)
    dias_solicitados := (fecha_fin::date - fecha_inicio::date) + 1;

    -- Iterar por cada día en el rango solicitado (de inicio a fin, ambos inclusivos)
    FOR fecha_actual IN SELECT generate_series(fecha_inicio::date, fecha_fin::date, INTERVAL '1 day')::date LOOP
        -- Iterar por cada grupo en el array
        FOREACH grupo_id IN ARRAY p_array_ids LOOP

            -- Contar equipos disponibles: total operativos - ocupados - en mantenimiento
            SELECT COUNT(*)
            INTO disponibilidad
            FROM public.equipos e
            WHERE e.id_grupo_equipo = grupo_id
              AND e.estado_eliminado = FALSE
              AND e.estado_equipo = 'operativo'
              AND dias_solicitados <= COALESCE(e.tiempo_max_prestamo, 9999)
              -- Excluir equipos ocupados en reservas activas ese día
              AND NOT EXISTS (
                  SELECT 1
                  FROM public.detalles_prestamos dp
                  INNER JOIN public.prestamos p ON dp.id_prestamo = p.id_prestamo
                  WHERE dp.id_equipo = e.id_equipo
                    AND p.estado_eliminado = FALSE
                    AND p.estado_prestamo IN ('pendiente', 'aprobado', 'activo')
                    AND fecha_actual BETWEEN p.fecha_prestamo_esperada::date AND p.fecha_devolucion_esperada::date
              )
              -- Excluir equipos en mantenimiento ese día
              AND NOT EXISTS (
                  SELECT 1
                  FROM public.detalles_mantenimientos dm
                  INNER JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
                  WHERE dm.id_equipo = e.id_equipo
                    AND m.estado_eliminado = FALSE
                    AND fecha_actual BETWEEN m.fecha_mantenimiento::date AND m.fecha_final_mantenimiento::date
              );

            -- Retornar la fecha con su disponibilidad
            RETURN QUERY SELECT (fecha_actual || ' 00:00:00')::timestamp without time zone, grupo_id, disponibilidad;
        END LOOP;
    END LOOP;
END;
$$;


--
-- Name: obtener_empresas_mantenimiento(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_empresas_mantenimiento() RETURNS TABLE(id_empresa_mantenimiento integer, nombre_empresa character varying, nombre_responsable_empresa character varying, apellido_responsable_empresa character varying, telefono_empresa character varying, nit_empresa character varying, direccion_empresa character varying)
    LANGUAGE plpgsql ROWS 100
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		em.id_empresa_mantenimiento,
        em.nombre,
        em.nombre_responsable,
        em.apellido_responsable,
        em.telefono,
        em.nit,
        em.direccion
    FROM
        public.empresas_mantenimiento AS em
    WHERE
        em.estado_eliminado = FALSE; 
                                    
END;
$$;


--
-- Name: obtener_equipos(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_equipos() RETURNS TABLE(id_equipo integer, nombre_grupo_equipo character varying, modelo_equipo character varying, marca_equipo character varying, codigo_imt_equipo integer, codigo_ucb_equipo character varying, numero_serial_equipo character varying, estado_equipo_equipo public.estado_equipo, ubicacion_equipo character varying, nombre_gavetero_equipo character varying, costo_referencia_equipo double precision, descripcion_equipo text, tiempo_max_prestamo_equipo integer, procedencia_equipo character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		e.id_equipo,
        ge.nombre AS nombre_grupo_equipo,
		ge.modelo as modelo_equipo,
		ge.marca as marca_equipo,
        e.codigo_imt AS codigo_imt_equipo,
        e.codigo_ucb AS codigo_ucb_equipo,
        e.numero_serial AS numero_serial_equipo,
        e.estado_equipo AS estado_equipo_equipo,
        e.ubicacion AS ubicacion_equipo,
        g.nombre AS nombre_gavetero_equipo, 
        e.costo_referencia AS costo_referencia_equipo,
        e.descripcion AS descripcion_equipo,
        e.tiempo_max_prestamo AS tiempo_max_prestamo_equipo,
        e.procedencia AS procedencia_equipo
    FROM
        public.equipos AS e
    left JOIN
        public.grupos_equipos AS ge 
		ON e.id_grupo_equipo = ge.id_grupo_equipo
        AND ge.estado_eliminado = FALSE  
    LEFT JOIN
        public.gaveteros AS g 
		ON e.id_gavetero = g.id_gavetero 
        and g.estado_eliminado=false                       
    WHERE
        e.estado_eliminado = FALSE;
END;
$$;


--
-- Name: obtener_equipos_necesitan_mantenimiento(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_equipos_necesitan_mantenimiento() RETURNS TABLE(id_equipo integer, codigo_imt integer, nombre character varying, estado_equipo public.estado_equipo, ubicacion character varying, ultima_fecha_mantenimiento date)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		e.id_equipo,
        e.codigo_imt,
        ge.nombre,
        e.estado_equipo,
        e.ubicacion,
        COALESCE(MAX(m.fecha_mantenimiento), e.fecha_ingreso_equipo) AS ultima_fecha_mantenimiento
    FROM equipos e
    LEFT JOIN detalles_mantenimientos as dm
        ON dm.id_equipo = e.id_equipo AND dm.estado_eliminado = false
    INNER JOIN grupos_equipos as ge
        ON ge.id_grupo_equipo = e.id_grupo_equipo
    LEFT JOIN mantenimientos as m
        ON m.id_mantenimiento = dm.id_mantenimiento AND m.estado_eliminado = false
    WHERE e.estado_eliminado = false
    GROUP BY
        e.codigo_imt, ge.nombre, e.estado_equipo, e.ubicacion, e.fecha_ingreso_equipo
    HAVING
        (
            
            e.estado_equipo IN ('parcialmente_operativo', 'inoperativo')
            OR
            
            (MAX(m.fecha_mantenimiento) IS NOT NULL AND EXTRACT(MONTH FROM AGE(CURRENT_DATE, MAX(m.fecha_mantenimiento))) > 4)
            OR
            
            (MAX(m.fecha_mantenimiento) IS NULL AND EXTRACT(MONTH FROM AGE(CURRENT_DATE, e.fecha_ingreso_equipo)) > 4)
        )
    ORDER BY
        ultima_fecha_mantenimiento DESC;

END;
$$;


--
-- Name: obtener_fechas_no_disponibles_por_id_grupos_equipos(timestamp without time zone, timestamp without time zone, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_fechas_no_disponibles_por_id_grupos_equipos(fecha_inicio timestamp without time zone, fecha_fin timestamp without time zone, json_input jsonb) RETURNS TABLE(id_grupo_equipo integer, fecha_no_disponible timestamp without time zone, cantidad_disponible bigint)
    LANGUAGE plpgsql
    AS $$
DECLARE
    fecha_actual date;
    grupo_key text;
    cantidad_requerida integer;
    disponibilidad bigint;
    grupo_array text[];
    dias_solicitados integer;
BEGIN
    -- Convertir jsonb_object_keys a array correctamente y ordenar
    grupo_array := array_agg(key ORDER BY key::integer) FROM jsonb_object_keys(json_input) AS t(key);
    
    -- Calcular días solicitados
    dias_solicitados := (fecha_fin::date - fecha_inicio::date);
    
    -- Iterar por cada día en el rango
    FOR fecha_actual IN SELECT generate_series(fecha_inicio::date, fecha_fin::date, INTERVAL '1 day')::date LOOP
        -- Iterar por cada grupo en el JSON usando el array pre-convertido
        FOREACH grupo_key IN ARRAY grupo_array LOOP
            cantidad_requerida := (json_input ->> grupo_key)::integer;
            
            -- Contar equipos disponibles para este grupo en esta fecha
            -- que CUMPLEN con: tiempo_solicitado <= tiempo_max_prestamo
            SELECT COUNT(*)
            INTO disponibilidad
            FROM (
                -- Obtener todos los equipos activos del grupo que cumplen restricción de tiempo
                SELECT e.id_equipo
                FROM public.equipos e
                WHERE e.id_grupo_equipo = grupo_key::integer
                  AND e.estado_eliminado = FALSE
                  AND e.estado_equipo = 'operativo'
                  AND dias_solicitados <= e.tiempo_max_prestamo
                
                EXCEPT
                
                -- Restar equipos ocupados en préstamos activos en esta fecha
                SELECT DISTINCT dp.id_equipo
                FROM public.detalles_prestamos dp
                INNER JOIN public.prestamos p ON dp.id_prestamo = p.id_prestamo
                WHERE p.estado_eliminado = FALSE
                  AND p.estado_prestamo IN ('pendiente', 'aprobado', 'activo')
                  AND fecha_actual BETWEEN p.fecha_prestamo_esperada::date AND p.fecha_devolucion_esperada::date
                
                EXCEPT
                
                -- Restar equipos en mantenimiento en esta fecha
                SELECT DISTINCT dm.id_equipo
                FROM public.detalles_mantenimientos dm
                INNER JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
                WHERE m.estado_eliminado = FALSE
                  AND fecha_actual BETWEEN m.fecha_mantenimiento AND m.fecha_final_mantenimiento
            ) equipos_disponibles;
            
            -- Si no hay suficientes equipos disponibles, registrar esta fecha como no disponible
            IF disponibilidad < cantidad_requerida THEN
                RETURN QUERY SELECT grupo_key::integer, (fecha_actual || ' 00:00:00')::timestamp without time zone, disponibilidad;
            END IF;
        END LOOP;
    END LOOP;
END;
$$;


--
-- Name: obtener_gaveteros(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_gaveteros() RETURNS TABLE(id_gavetero integer, nombre_gavetero character varying, tipo_gavetero character varying, nombre_mueble character varying, longitud_gavetero double precision, profundidad_gavetero double precision, altura_gavetero double precision)
    LANGUAGE plpgsql ROWS 100
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		g.id_gavetero,
        g.nombre AS nombre_gavetero,
        g.tipo AS tipo_gavetero,
        m.nombre AS nombre_mueble,
        g.longitud AS longitud_gavetero,
        g.profundidad AS profundidad_gavetero,
        g.altura AS altura_gavetero
    FROM
        public.gaveteros AS g
    left JOIN
        public.muebles AS m ON g.id_mueble = m.id_mueble
								and m.estado_eliminado=false
    WHERE
        g.estado_eliminado = FALSE;

END;
$$;


--
-- Name: obtener_grupo_equipo_especifico_por_id(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_grupo_equipo_especifico_por_id(id_grupo_equipo_input integer) RETURNS TABLE(id_grupo_equipo integer, nombre_grupo_equipo character varying, modelo_grupo_equipo character varying, marca_grupo_equipo character varying, descripcion_grupo_equipo text, url_data_sheet_grupo_equipo text, nombre_categoria character varying, url_imagen_grupo_equipo text, cantidad_grupo_equipo integer, costo_promedio numeric)
    LANGUAGE plpgsql
    AS $$
BEGIN

    RETURN QUERY
    SELECT 
        ge.id_grupo_equipo,
        ge.nombre,
        ge.modelo,
        ge.marca,
        ge.descripcion,
        ge.url_data_sheet,
        c.nombre as categoria,
        ge.url_imagen,
        ge.cantidad,
        ge.costo_promedio
    FROM grupos_equipos as ge
    INNER JOIN categorias as c
        ON c.id_categoria = ge.id_categoria
    WHERE 
        ge.id_grupo_equipo = id_grupo_equipo_input
    LIMIT 1;
END;
$$;


--
-- Name: obtener_grupos_equipos(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_grupos_equipos() RETURNS TABLE(id_grupo_equipo integer, nombre_grupo_equipo character varying, modelo_grupo_equipo character varying, marca_grupo_equipo character varying, nombre_categoria character varying, cantidad_grupo_equipo integer, descripcion_grupo_equipo text, url_data_sheet_grupo_equipo text, url_imagen_grupo_equipo text, costo_promedio numeric)
    LANGUAGE plpgsql ROWS 100
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		ge.id_grupo_equipo as id_grupo_equipo,
        ge.nombre AS nombre_grupo_equipo,
        ge.modelo AS modelo_grupo_equipo,
        ge.marca AS marca_grupo_equipo,
        c.nombre AS nombre_categoria,
        ge.cantidad AS cantidad_grupo_equipo,
        ge.descripcion AS descripcion_grupo_equipo,
		ge.url_data_sheet as url_data_sheet_grupo_equipo,
		ge.url_imagen as url_imagen_grupo_equipo,
        ge.costo_promedio as costo_promedio
    FROM
        public.grupos_equipos AS ge
    Left JOIN
        public.categorias AS c 
		ON ge.id_categoria = c.id_categoria
                             AND c.estado_eliminado = FALSE 
    WHERE
        ge.estado_eliminado = FALSE; 
END;
$$;


--
-- Name: obtener_grupos_equipos_por_nombre_y_categoria(text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_grupos_equipos_por_nombre_y_categoria(nombre_grupo_equipo_input text, categoria_input text) RETURNS TABLE(id_grupo_equipo integer, nombre_grupo_equipo character varying, modelo_grupo_equipo character varying, marca_grupo_equipo character varying, nombre_categoria character varying, url_imagen_grupo_equipo text, url_data_sheet_grupo_equipo text, descripcion_grupo_equipo text, cantidad_grupo_equipo integer, costo_promedio numeric)
    LANGUAGE plpgsql
    AS $$
BEGIN

    RETURN QUERY
    SELECT 
        ge.id_grupo_equipo,
        ge.nombre,
        ge.modelo,
        ge.marca,
        c.nombre as categoria,
        ge.url_imagen,
        ge.url_data_sheet,
        ge.descripcion,
        ge.cantidad,
        ge.costo_promedio
    FROM grupos_equipos as ge
    INNER JOIN categorias as c
        ON c.id_categoria = ge.id_categoria
    WHERE 
        (REPLACE(LOWER(ge.nombre),' ','') LIKE '%' || REPLACE(LOWER(nombre_grupo_equipo_input),' ','') || '%' OR nombre_grupo_equipo_input is NULL)  
        AND (REPLACE(LOWER(c.nombre),' ','') LIKE '%' || REPLACE(LOWER(categoria_input),' ','') || '%' OR categoria_input is NULL)  
        AND ge.estado_eliminado = false;
END;
$$;


--
-- Name: obtener_mantenimientos(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_mantenimientos() RETURNS TABLE(id_mantenimiento integer, nombre_empresa_mantenimiento character varying, fecha_mantenimiento date, fecha_final_mantenimiento date, costo_mantenimiento double precision, descripcion_mantenimiento text, tipo_detalle_mantenimiento character varying, nombre_grupo_equipo character varying, codigo_imt_equipo integer, descripcion_equipo text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		m.id_mantenimiento,
        em.nombre AS nombre_empresa_mantenimiento,
        m.fecha_mantenimiento,
        m.fecha_final_mantenimiento,
        m.costo AS costo_mantenimiento,
        m.descripcion AS descripcion_mantenimiento,
        dm.tipo_mantenimiento AS tipo_detalle_mantenimiento,
        ge.nombre AS nombre_grupo_equipo,
        e.codigo_imt AS codigo_imt_equipo,
		dm.descripcion as descripcion_equipo
    FROM
        public.mantenimientos AS m
    LEFT JOIN
        public.empresas_mantenimiento AS em
            ON m.id_empresa = em.id_empresa_mantenimiento AND em.estado_eliminado = FALSE
    LEFT JOIN
        public.detalles_mantenimientos AS dm
            ON m.id_mantenimiento = dm.id_mantenimiento AND dm.estado_eliminado = FALSE
    LEFT JOIN
        public.equipos AS e
            ON dm.id_equipo = e.id_equipo AND e.estado_eliminado = FALSE
    LEFT JOIN
        public.grupos_equipos AS ge
            ON e.id_grupo_equipo = ge.id_grupo_equipo AND ge.estado_eliminado = FALSE
    WHERE
        m.estado_eliminado = FALSE
	order by m.id_mantenimiento; 
END;
$$;


--
-- Name: obtener_muebles(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_muebles() RETURNS TABLE(id_mueble integer, nombre_mueble character varying, numero_gaveteros_mueble integer, ubicacion_mueble character varying, tipo_mueble character varying, costo_mueble double precision, longitud_mueble double precision, profundidad_mueble double precision, altura_mueble double precision)
    LANGUAGE plpgsql ROWS 100
    AS $$
BEGIN
    RETURN QUERY
    SELECT
		m.id_mueble,
        m.nombre AS nombre_mueble,
        m.numero_gaveteros AS numero_gaveteros_mueble,
        m.ubicacion AS ubicacion_mueble,
        m.tipo AS tipo_mueble,
        m.costo AS costo_mueble,
        m.longitud AS longitud_mueble,
        m.profundidad AS profundidad_mueble,
        m.altura AS altura_mueble
    FROM
        public.muebles AS m
    WHERE
        m.estado_eliminado = FALSE;
END;
$$;


--
-- Name: obtener_numero_equipos_disponibles_por_id_y_fechas(integer, timestamp without time zone, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_numero_equipos_disponibles_por_id_y_fechas(id_grupo_equipo_input integer, fecha_prestamo_esperada_input timestamp without time zone, fecha_devolucion_esperada_input timestamp without time zone) RETURNS TABLE(cantidad_disponible bigint)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT COUNT(e.id_equipo) AS cantidad_equipos_disponibles
    FROM public.equipos e
    WHERE
        e.id_grupo_equipo = id_grupo_equipo_input

        AND e.estado_eliminado = FALSE
        AND e.estado_equipo = 'operativo'

        AND NOT EXISTS (
            SELECT 1
            FROM public.detalles_prestamos dp
            JOIN public.prestamos pr ON dp.id_prestamo = pr.id_prestamo
            WHERE dp.id_equipo = e.id_equipo
              AND pr.estado_eliminado = FALSE
              AND pr.estado_prestamo IN ('pendiente', 'aprobado', 'activo') 
              AND (
                  
                  fecha_prestamo_esperada_input < pr.fecha_devolucion_esperada AND
                  fecha_devolucion_esperada_input > pr.fecha_prestamo_esperada
              )
        )

        AND NOT EXISTS (
            SELECT 1
            FROM public.detalles_mantenimientos dm
            JOIN public.mantenimientos m ON dm.id_mantenimiento = m.id_mantenimiento
            WHERE dm.id_equipo = e.id_equipo
              AND m.estado_eliminado = FALSE 
              AND (
                  
                  fecha_prestamo_esperada_input < m.fecha_final_mantenimiento AND
                  fecha_devolucion_esperada_input > m.fecha_mantenimiento
              )
        );
END;
$$;


--
-- Name: obtener_prestamos(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_prestamos() RETURNS TABLE(id_prestamo integer, carnet character varying, nombre character varying, apellido_paterno character varying, telefono character varying, nombre_grupo_equipo character varying, codigo_imt integer, fecha_solicitud timestamp without time zone, fecha_prestamo_esperada timestamp without time zone, fecha_prestamo timestamp without time zone, fecha_devolucion_esperada timestamp without time zone, fecha_devolucion timestamp without time zone, observacion text, estado_prestamo public.estado_prestamo, ubicacion_equipo character varying, nombre_gavetero character varying, nombre_mueble character varying, ubicacion_mueble character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
	SELECT
	p.id_prestamo,
	p.carnet,
	u.nombre,
	u.apellido_paterno,
	u.telefono,
	ge.nombre as nombre_grupo_equipo,
	e.codigo_imt,
	p.fecha_solicitud, 
	p.fecha_prestamo_esperada,
	p.fecha_prestamo,
	p.fecha_devolucion_esperada,
	p.fecha_devolucion,
	p.observacion, 
	p.estado_prestamo,
	e.ubicacion as ubicacion_equipo,
	ga.nombre as nombre_gavetero,
	mu.nombre as nombre_mueble,
	mu.ubicacion as ubicacion_mueble
	FROM public.prestamos as p
	left join detalles_prestamos as dp
	on dp.id_prestamo=p.id_prestamo
	and dp.estado_eliminado=false
	left join equipos as e
	on dp.id_equipo=e.id_equipo
	and e.estado_eliminado=false
	left join grupos_equipos as ge
	on ge.id_grupo_equipo=e.id_grupo_equipo
	and ge.estado_eliminado=false
	left join usuarios as u
	on u.carnet=p.carnet
	and u.estado_eliminado=false
	left join gaveteros as ga
	on ga.id_gavetero=e.id_gavetero
	and ga.estado_eliminado=false
	left join muebles as mu
	on mu.id_mueble=ga.id_mueble
	and mu.estado_eliminado=false
	
	where p.estado_eliminado=false
	order by fecha_solicitud desc;
END;
$$;


--
-- Name: obtener_prestamos_por_carnet_y_estado_prestamo(character varying, public.estado_prestamo); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_prestamos_por_carnet_y_estado_prestamo(p_carnet_input character varying, p_estado_input public.estado_prestamo) RETURNS TABLE(id_prestamo integer, carnet character varying, nombre character varying, apellido_paterno character varying, telefono character varying, nombre_grupo_equipo character varying, codigo_imt integer, fecha_solicitud timestamp without time zone, fecha_prestamo_esperada timestamp without time zone, fecha_prestamo timestamp without time zone, fecha_devolucion_esperada timestamp without time zone, fecha_devolucion timestamp without time zone, observacion text, estado_prestamo public.estado_prestamo, ubicacion_equipo character varying, nombre_gavetero character varying, nombre_mueble character varying, ubicacion_mueble character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        p.id_prestamo                          AS id_prestamo,
        p.carnet                               AS carnet,
        u.nombre                               AS nombre,
        u.apellido_paterno                     AS apellido_paterno,
        u.telefono                             AS telefono,
        ge.nombre                              AS nombre_grupo_equipo,
        e.codigo_imt                           AS codigo_imt,
        p.fecha_solicitud                      AS fecha_solicitud,
        p.fecha_prestamo_esperada              AS fecha_prestamo_esperada,
        p.fecha_prestamo                       AS fecha_prestamo,
        p.fecha_devolucion_esperada            AS fecha_devolucion_esperada,
        p.fecha_devolucion                     AS fecha_devolucion,
        p.observacion                          AS observacion,
        p.estado_prestamo                      AS estado_prestamo,
		e.ubicacion								AS ubicacion_equipo,
		ga.nombre								AS nombre_gavetero,
		mu.nombre								AS nombre_mueble,
		mu.ubicacion							AS ubicacion_mueble
    FROM public.prestamos p
    INNER JOIN public.detalles_prestamos dp
        ON dp.id_prestamo = p.id_prestamo
    INNER JOIN public.equipos e
        ON dp.id_equipo = e.id_equipo
    INNER JOIN public.grupos_equipos ge
        ON e.id_grupo_equipo = ge.id_grupo_equipo
    INNER JOIN public.usuarios u
        ON u.carnet = p.carnet
	LEFT JOIN public.gaveteros as ga
		ON ga.id_gavetero=e.id_gavetero
	LEFT JOIN public.muebles as mu
		ON mu.id_mueble=ga.id_mueble
    WHERE NOT p.estado_eliminado
      AND p.carnet = p_carnet_input
      AND p.estado_prestamo = p_estado_input
    ORDER BY p.fecha_solicitud DESC;
END;
$$;


--
-- Name: obtener_ubicaciones_grupos_equipos_por_nombre(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_ubicaciones_grupos_equipos_por_nombre(nombre_grupo_equipo_input text) RETURNS TABLE(id_grupo_equipo integer, codigo_imt integer, nombre character varying, modelo character varying, marca character varying, ubicacion character varying, categoria character varying, url_imagen text)
    LANGUAGE plpgsql
    AS $$
BEGIN

    RETURN QUERY
    SELECT 
		ge.id_grupo_equipo,
		e.codigo_imt,
        ge.nombre,
        ge.modelo,
        ge.marca,
		e.ubicacion,
        c.nombre as categoria,
        ge.url_imagen
    FROM grupos_equipos as ge
	inner join equipos as e
	on e.id_grupo_equipo=ge.id_grupo_equipo
    INNER JOIN categorias as c
    ON c.id_categoria = ge.id_categoria
	Left join gaveteros as ga
	on e.id_gavetero=ga.id_gavetero
	inner join muebles as mu
	on mu.id_mueble=ga.id_mueble
    WHERE 
        (REPLACE(LOWER(ge.nombre),' ','') LIKE '%' || REPLACE(LOWER(nombre_grupo_equipo_input),' ','') || '%');
END;
$$;


--
-- Name: obtener_usuario_iniciar_sesion(character varying, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_usuario_iniciar_sesion(email_input character varying, contrasena_input text) RETURNS TABLE(carnet character varying, nombre character varying, apellido_paterno character varying, apellido_materno character varying, rol public.tipo_usuario, carrera character varying, email character varying, telefono character varying, telefono_referencia character varying, nombre_referencia character varying, email_referencia character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.carnet,
		u.nombre,
		u.apellido_paterno,
		u.apellido_materno,
        u.rol,
        c.nombre,
		u.email,
        u.telefono,
        u.telefono_referencia,
        u.nombre_referencia,
        u.email_referencia
    FROM usuarios as u
	inner join carreras as c
	on c.id_carrera=u.id_carrera
    WHERE 
	u.email=email_input and
	u.contrasena=contrasena_input and 
	u.estado_eliminado=false;
END;
$$;


--
-- Name: obtener_usuario_por_carnet(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_usuario_por_carnet(carnet_input text) RETURNS TABLE(carnet character varying, nombre character varying, apellido_paterno character varying, apellido_materno character varying, rol public.tipo_usuario, carrera character varying, email character varying, contrasena text, telefono character varying, telefono_referencia character varying, nombre_referencia character varying, email_referencia character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.carnet,
        u.nombre,
        u.apellido_paterno,
        u.apellido_materno,
        u.rol,
        c.nombre,
		u.email,
		u.contrasena,
        u.telefono,
        u.telefono_referencia,
        u.nombre_referencia,
        u.email_referencia
    FROM usuarios as u
	inner join carreras as c
	on c.id_carrera=u.id_carrera
    WHERE u.carnet = carnet_input
	and u.estado_eliminado=false;
END;
$$;


--
-- Name: obtener_usuarios(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.obtener_usuarios() RETURNS TABLE(carnet character varying, nombre character varying, apellido_paterno character varying, apellido_materno character varying, carrera character varying, rol public.tipo_usuario, email character varying, telefono character varying, telefono_referencia character varying, nombre_referencia character varying, email_referencia character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
	SELECT u.carnet, u.nombre, u.apellido_paterno, u.apellido_materno, c.nombre, u.rol, u.email, u.telefono, u.telefono_referencia, u.nombre_referencia, u.email_referencia
	FROM public.usuarios as u
	left join carreras as c
	on c.id_carrera=u.id_carrera
	and c.estado_eliminado=false
	where u.estado_eliminado=false;
END;
$$;


--
-- Name: recalcular_costo_promedio_todos_grupos(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.recalcular_costo_promedio_todos_grupos()
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_grupo_id integer;
    v_costo_promedio numeric(10,2);
BEGIN
    -- Iterar sobre todos los grupos de equipos
    FOR v_grupo_id IN SELECT id_grupo_equipo FROM public.grupos_equipos ORDER BY id_grupo_equipo LOOP
        
        -- Calcular promedio de equipos operativos no eliminados
        SELECT COALESCE(AVG(costo_referencia), 0)
        INTO v_costo_promedio
        FROM public.equipos
        WHERE id_grupo_equipo = v_grupo_id
          AND estado_eliminado = FALSE
          AND estado_equipo = 'operativo';
        
        -- Actualizar el grupo
        UPDATE public.grupos_equipos
        SET costo_promedio = v_costo_promedio
        WHERE id_grupo_equipo = v_grupo_id;
        
        RAISE NOTICE 'Grupo % actualizado: costo_promedio = %', v_grupo_id, v_costo_promedio;
    END LOOP;
    
    RAISE NOTICE 'Todos los costos promedios han sido recalculados.';
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: aggregatedcounter; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.aggregatedcounter (
    id bigint NOT NULL,
    key text NOT NULL,
    value bigint NOT NULL,
    expireat timestamp with time zone
);


--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.aggregatedcounter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.aggregatedcounter_id_seq OWNED BY hangfire.aggregatedcounter.id;


--
-- Name: counter; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.counter (
    id bigint NOT NULL,
    key text NOT NULL,
    value bigint NOT NULL,
    expireat timestamp with time zone
);


--
-- Name: counter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.counter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: counter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.counter_id_seq OWNED BY hangfire.counter.id;


--
-- Name: hash; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.hash (
    id bigint NOT NULL,
    key text NOT NULL,
    field text NOT NULL,
    value text,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: hash_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.hash_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hash_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.hash_id_seq OWNED BY hangfire.hash.id;


--
-- Name: job; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.job (
    id bigint NOT NULL,
    stateid bigint,
    statename text,
    invocationdata jsonb NOT NULL,
    arguments jsonb NOT NULL,
    createdat timestamp with time zone NOT NULL,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: job_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.job_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: job_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.job_id_seq OWNED BY hangfire.job.id;


--
-- Name: jobparameter; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.jobparameter (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    name text NOT NULL,
    value text,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: jobparameter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.jobparameter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: jobparameter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.jobparameter_id_seq OWNED BY hangfire.jobparameter.id;


--
-- Name: jobqueue; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.jobqueue (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    queue text NOT NULL,
    fetchedat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: jobqueue_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.jobqueue_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: jobqueue_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.jobqueue_id_seq OWNED BY hangfire.jobqueue.id;


--
-- Name: list; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.list (
    id bigint NOT NULL,
    key text NOT NULL,
    value text,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: list_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.list_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: list_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.list_id_seq OWNED BY hangfire.list.id;


--
-- Name: lock; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.lock (
    resource text NOT NULL,
    updatecount integer DEFAULT 0 NOT NULL,
    acquired timestamp with time zone
);


--
-- Name: schema; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.schema (
    version integer NOT NULL
);


--
-- Name: server; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.server (
    id text NOT NULL,
    data jsonb,
    lastheartbeat timestamp with time zone NOT NULL,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: set; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.set (
    id bigint NOT NULL,
    key text NOT NULL,
    score double precision NOT NULL,
    value text NOT NULL,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: set_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.set_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: set_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.set_id_seq OWNED BY hangfire.set.id;


--
-- Name: state; Type: TABLE; Schema: hangfire; Owner: -
--

CREATE TABLE hangfire.state (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    name text NOT NULL,
    reason text,
    createdat timestamp with time zone NOT NULL,
    data jsonb,
    updatecount integer DEFAULT 0 NOT NULL
);


--
-- Name: state_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: -
--

CREATE SEQUENCE hangfire.state_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: state_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: -
--

ALTER SEQUENCE hangfire.state_id_seq OWNED BY hangfire.state.id;


--
-- Name: accesorios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accesorios (
    id_accesorio integer NOT NULL,
    nombre character varying(255) NOT NULL,
    descripcion text,
    modelo character varying(255) NOT NULL,
    url_data_sheet text,
    precio double precision,
    id_equipo integer NOT NULL,
    tipo character varying(255),
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: COLUMN accesorios.id_accesorio; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.accesorios.id_accesorio IS 'Código del accesorio';


--
-- Name: Accesorio_Id_Accesorio_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.accesorios ALTER COLUMN id_accesorio ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Accesorio_Id_Accesorio_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: categorias; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.categorias (
    id_categoria integer NOT NULL,
    nombre character varying(255) NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: Categoria_ID_Categoria_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.categorias ALTER COLUMN id_categoria ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Categoria_ID_Categoria_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: componentes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.componentes (
    id_componente integer NOT NULL,
    descripcion text,
    modelo character varying(255) NOT NULL,
    url_data_sheet text,
    tipo character varying(255),
    precio_referencia double precision,
    nombre character varying(255) NOT NULL,
    id_equipo integer NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: COLUMN componentes.id_componente; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.componentes.id_componente IS 'Código del componente';


--
-- Name: Componente_Id_Componente_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.componentes ALTER COLUMN id_componente ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Componente_Id_Componente_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: empresas_mantenimiento; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.empresas_mantenimiento (
    id_empresa_mantenimiento integer NOT NULL,
    nombre character varying(255) NOT NULL,
    direccion character varying(512),
    telefono character varying(64),
    nit character varying(255),
    estado_eliminado boolean DEFAULT false NOT NULL,
    nombre_responsable character varying(64),
    apellido_responsable character varying(64)
);


--
-- Name: COLUMN empresas_mantenimiento.id_empresa_mantenimiento; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.empresas_mantenimiento.id_empresa_mantenimiento IS 'Código empresa';


--
-- Name: Empresa_Mantenimiento_Id_Empresa_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.empresas_mantenimiento ALTER COLUMN id_empresa_mantenimiento ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Empresa_Mantenimiento_Id_Empresa_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: equipos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.equipos (
    id_equipo integer NOT NULL,
    id_grupo_equipo integer NOT NULL,
    codigo_imt integer NOT NULL,
    descripcion text,
    estado_equipo public.estado_equipo DEFAULT 'operativo'::public.estado_equipo NOT NULL,
    numero_serial character varying(255),
    ubicacion character varying(255),
    costo_referencia double precision DEFAULT 0,
    tiempo_max_prestamo integer DEFAULT 9999,
    procedencia character varying(255),
    id_gavetero integer,
    estado_eliminado boolean DEFAULT false NOT NULL,
    fecha_ingreso_equipo date DEFAULT CURRENT_DATE NOT NULL,
    codigo_ucb character varying,
    id_ambiente integer,
    id_procedencia integer
);


--
-- Name: Equipo_Id_equipo_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.equipos ALTER COLUMN id_equipo ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Equipo_Id_equipo_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: gaveteros; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.gaveteros (
    id_gavetero integer NOT NULL,
    nombre character varying(255) NOT NULL,
    tipo character varying(255),
    estado_eliminado boolean DEFAULT false NOT NULL,
    id_mueble integer NOT NULL,
    longitud double precision,
    profundidad double precision,
    altura double precision
);


--
-- Name: Gavetero_Id_Gavetero_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.gaveteros ALTER COLUMN id_gavetero ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Gavetero_Id_Gavetero_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: grupos_equipos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.grupos_equipos (
    id_grupo_equipo integer NOT NULL,
    nombre character varying(256) NOT NULL,
    modelo character varying(512) NOT NULL,
    url_data_sheet text,
    cantidad integer DEFAULT 0 NOT NULL,
    marca character varying(256) NOT NULL,
    id_categoria integer NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    url_imagen text NOT NULL,
    descripcion text NOT NULL,
    costo_promedio numeric(10,2) DEFAULT 0,
    tiempo_max_prestamo_dias integer DEFAULT 7 NOT NULL,
    CONSTRAINT ck_grupos_equipos_tiempo_max_prestamo CHECK (((tiempo_max_prestamo_dias >= 1) AND (tiempo_max_prestamo_dias <= 365)))
);


--
-- Name: COLUMN grupos_equipos.descripcion; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.grupos_equipos.descripcion IS 'Esto se mostrar en la pagina web';


--
-- Name: Grupo_Equipo_Id_Grupo_equipo_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.grupos_equipos ALTER COLUMN id_grupo_equipo ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Grupo_Equipo_Id_Grupo_equipo_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: mantenimientos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mantenimientos (
    id_mantenimiento integer NOT NULL,
    descripcion text,
    costo double precision,
    fecha_mantenimiento date NOT NULL,
    id_empresa integer NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    fecha_final_mantenimiento date NOT NULL
);


--
-- Name: Mantenimiento_Id_Mantenimiento_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.mantenimientos ALTER COLUMN id_mantenimiento ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Mantenimiento_Id_Mantenimiento_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: muebles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.muebles (
    id_mueble integer NOT NULL,
    nombre character varying(255) NOT NULL,
    tipo character varying(255),
    ubicacion character varying(255),
    numero_gaveteros integer DEFAULT 0 NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    longitud double precision,
    profundidad double precision,
    altura double precision,
    costo double precision,
    id_ambiente integer
);


--
-- Name: COLUMN muebles.id_mueble; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.muebles.id_mueble IS 'Código del mueble';


--
-- Name: Mueble_Id_Mueble_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.muebles ALTER COLUMN id_mueble ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Mueble_Id_Mueble_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: prestamos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.prestamos (
    id_prestamo integer NOT NULL,
    fecha_solicitud timestamp without time zone DEFAULT (now() AT TIME ZONE 'America/La_Paz'::text) NOT NULL,
    fecha_prestamo timestamp without time zone,
    fecha_devolucion_esperada timestamp without time zone NOT NULL,
    observacion text,
    estado_prestamo public.estado_prestamo DEFAULT 'pendiente'::public.estado_prestamo NOT NULL,
    carnet character varying(64) NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    fecha_devolucion timestamp without time zone,
    fecha_prestamo_esperada timestamp without time zone NOT NULL,
    id_contrato integer,
    recordatorio_enviado boolean DEFAULT false NOT NULL,
    destino_prestamo character varying(50) DEFAULT 'Universidad'::character varying NOT NULL,
    id_carrera integer,
    nombre_materia character varying(255),
    autorizado_por character varying(255),
    entregado_por character varying(255),
    motivo_rechazo character varying(1024),
    guardado boolean DEFAULT false NOT NULL
);


--
-- Name: COLUMN prestamos.id_prestamo; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.prestamos.id_prestamo IS 'Código del préstamo';


--
-- Name: Prestamo_Id_Prestamo_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.prestamos ALTER COLUMN id_prestamo ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Prestamo_Id_Prestamo_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: ambientes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ambientes (
    id_ambiente integer NOT NULL,
    nombre character varying(255) NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    carnet_administrador character varying(64)
);


--
-- Name: ambientes_id_ambiente_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.ambientes ALTER COLUMN id_ambiente ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.ambientes_id_ambiente_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: audit_logs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_logs (
    id integer NOT NULL,
    admin_carnet character varying(20) NOT NULL,
    admin_nombre text NOT NULL,
    accion character varying(50) NOT NULL,
    entidad character varying(100) NOT NULL,
    entidad_id text,
    detalle text,
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: audit_logs_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.audit_logs_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: audit_logs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.audit_logs_id_seq OWNED BY public.audit_logs.id;


--
-- Name: avisos_disponibilidad; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.avisos_disponibilidad (
    id_aviso integer NOT NULL,
    carnet_usuario character varying(20) NOT NULL,
    id_grupo_equipo integer NOT NULL,
    fecha timestamp without time zone NOT NULL,
    cantidad integer DEFAULT 1 NOT NULL,
    notificado boolean DEFAULT false NOT NULL,
    fecha_creacion timestamp with time zone DEFAULT now() NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: avisos_disponibilidad_id_aviso_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.avisos_disponibilidad ALTER COLUMN id_aviso ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.avisos_disponibilidad_id_aviso_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: carreras; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.carreras (
    id_carrera integer NOT NULL,
    nombre character varying(255) NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: carrera_id_carrera_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.carrera_id_carrera_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: carrera_id_carrera_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.carrera_id_carrera_seq OWNED BY public.carreras.id_carrera;


--
-- Name: carreras_id_carrera_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.carreras ALTER COLUMN id_carrera ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.carreras_id_carrera_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: codigos_autenticacion; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.codigos_autenticacion (
    hash character varying(64) NOT NULL,
    tipo character varying(20) NOT NULL,
    email character varying(255) NOT NULL,
    google_id character varying(255) NOT NULL,
    nombre character varying(64) NOT NULL,
    apellido_paterno character varying(64) NOT NULL,
    apellido_materno character varying(64) NOT NULL,
    expira timestamp with time zone NOT NULL,
    usado boolean DEFAULT false NOT NULL
);


--
-- Name: comentarios_equipos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.comentarios_equipos (
    id_comentario_equipo integer NOT NULL,
    id_grupo_equipo integer NOT NULL,
    carnet_usuario character varying(20) NOT NULL,
    contenido character varying(1024) NOT NULL,
    fecha_creacion timestamp with time zone DEFAULT now() NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    id_comentario_padre integer,
    likes integer DEFAULT 0 NOT NULL,
    liked_by text DEFAULT ''::text NOT NULL
);


--
-- Name: comentarios_equipos_id_comentario_equipo_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.comentarios_equipos ALTER COLUMN id_comentario_equipo ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.comentarios_equipos_id_comentario_equipo_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: configuraciones_sistema; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.configuraciones_sistema (
    id_configuracion integer NOT NULL,
    monto_minimo_contrato numeric NOT NULL,
    horario_inicio_minutos integer NOT NULL,
    horario_fin_minutos integer NOT NULL,
    nombre_jefe_carrera text NOT NULL,
    firma_jefe_carrera_base64 text NOT NULL,
    tiempo_minimo_reserva_minutos integer NOT NULL,
    tiempo_recordatorio_previo_minutos integer NOT NULL,
    minutos_gracia_atraso integer NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    horarios jsonb DEFAULT '[]'::jsonb NOT NULL,
    carnet_jefe_carrera character varying(20)
);


--
-- Name: configuraciones_sistema_id_configuracion_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.configuraciones_sistema ALTER COLUMN id_configuracion ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.configuraciones_sistema_id_configuracion_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: contratos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.contratos (
    id integer NOT NULL,
    contrato text
);


--
-- Name: detalles_mantenimientos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.detalles_mantenimientos (
    id_detalle_mantenimiento integer NOT NULL,
    id_mantenimiento integer NOT NULL,
    descripcion text,
    id_equipo integer NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    tipo_mantenimiento character varying
);


--
-- Name: detalles_mantenimientos_id_detalle_mantenimiento_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.detalles_mantenimientos_id_detalle_mantenimiento_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: detalles_mantenimientos_id_detalle_mantenimiento_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.detalles_mantenimientos_id_detalle_mantenimiento_seq OWNED BY public.detalles_mantenimientos.id_detalle_mantenimiento;


--
-- Name: detalles_mantenimientos_id_detalle_mantenimiento_seq1; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.detalles_mantenimientos ALTER COLUMN id_detalle_mantenimiento ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.detalles_mantenimientos_id_detalle_mantenimiento_seq1
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: detalles_prestamos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.detalles_prestamos (
    id_detalle_prestamo integer NOT NULL,
    id_equipo integer,
    id_prestamo integer NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL,
    id_grupo_equipo integer NOT NULL,
    estado_equipo_retorno public.estado_equipo
);


--
-- Name: detalles_prestamos_id_detalle_prestamo_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.detalles_prestamos ALTER COLUMN id_detalle_prestamo ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.detalles_prestamos_id_detalle_prestamo_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: nombre_de_tu_tabla_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.contratos ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.nombre_de_tu_tabla_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: notificaciones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notificaciones (
    id_notificacion integer NOT NULL,
    carnet_usuario character varying(20) NOT NULL,
    tipo character varying(50) NOT NULL,
    titulo text NOT NULL,
    contenido text,
    detalle text,
    leido boolean DEFAULT false NOT NULL,
    fecha_envio timestamp with time zone DEFAULT now() NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: notificaciones_id_notificacion_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.notificaciones ALTER COLUMN id_notificacion ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.notificaciones_id_notificacion_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: procedencias; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.procedencias (
    id_procedencia integer NOT NULL,
    nombre character varying(255) NOT NULL,
    estado_eliminado boolean DEFAULT false NOT NULL
);


--
-- Name: procedencias_id_procedencia_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.procedencias ALTER COLUMN id_procedencia ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.procedencias_id_procedencia_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: usuarios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.usuarios (
    carnet character varying(64) NOT NULL,
    nombre character varying(64) NOT NULL,
    apellido_paterno character varying(64) NOT NULL,
    apellido_materno character varying(64) NOT NULL,
    rol public.tipo_usuario DEFAULT 'estudiante'::public.tipo_usuario NOT NULL,
    contrasena text NOT NULL,
    email character varying(255) NOT NULL,
    telefono character varying(32) NOT NULL,
    telefono_referencia character varying(32),
    nombre_referencia character varying(32),
    email_referencia character varying(255),
    estado_eliminado boolean DEFAULT false NOT NULL,
    id_carrera integer NOT NULL,
    imagen_frente_carnet bytea,
    imagen_atras_carnet bytea,
    refresh_token text,
    refresh_token_expiry timestamp with time zone,
    bloqueado boolean DEFAULT false NOT NULL,
    motivo_bloqueo text,
    imagen_firma bytea,
    email_verificado boolean DEFAULT true NOT NULL,
    google_id character varying(255),
    token_verificacion_hash character varying(64),
    token_verificacion_expira timestamp with time zone,
    imagen_perfil bytea
);


--
-- Name: vw_equipos_necesitan_mantenimiento; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.vw_equipos_necesitan_mantenimiento AS
 SELECT e.codigo_imt,
    ge.nombre AS grupo_equipo,
    e.estado_equipo,
    e.ubicacion,
    COALESCE(max(m.fecha_mantenimiento), e.fecha_ingreso_equipo) AS ultima_fecha_mantenimiento
   FROM (((public.equipos e
     LEFT JOIN public.detalles_mantenimientos dm ON (((dm.id_equipo = e.id_equipo) AND (dm.estado_eliminado = false))))
     JOIN public.grupos_equipos ge ON ((ge.id_grupo_equipo = e.id_grupo_equipo)))
     LEFT JOIN public.mantenimientos m ON (((m.id_mantenimiento = dm.id_mantenimiento) AND (m.estado_eliminado = false))))
  WHERE (e.estado_eliminado = false)
  GROUP BY e.codigo_imt, ge.nombre, e.estado_equipo, e.ubicacion, e.fecha_ingreso_equipo
 HAVING ((e.estado_equipo = ANY (ARRAY['parcialmente_operativo'::public.estado_equipo, 'inoperativo'::public.estado_equipo])) OR ((max(m.fecha_mantenimiento) IS NOT NULL) AND (EXTRACT(month FROM age((CURRENT_DATE)::timestamp with time zone, (max(m.fecha_mantenimiento))::timestamp with time zone)) > (4)::numeric)) OR ((max(m.fecha_mantenimiento) IS NULL) AND (EXTRACT(month FROM age((CURRENT_DATE)::timestamp with time zone, (e.fecha_ingreso_equipo)::timestamp with time zone)) > (4)::numeric)));


--
-- Name: vw_ubicaciones_grupos_equipos; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.vw_ubicaciones_grupos_equipos AS
 SELECT ge.id_grupo_equipo,
    e.codigo_imt,
    ge.nombre,
    ge.modelo,
    ge.marca,
    e.ubicacion,
    c.nombre AS categoria,
    ge.url_imagen
   FROM ((((public.grupos_equipos ge
     JOIN public.equipos e ON ((e.id_grupo_equipo = ge.id_grupo_equipo)))
     JOIN public.categorias c ON ((c.id_categoria = ge.id_categoria)))
     LEFT JOIN public.gaveteros ga ON ((e.id_gavetero = ga.id_gavetero)))
     JOIN public.muebles mu ON ((mu.id_mueble = ga.id_mueble)))
  WHERE ((ge.estado_eliminado = false) AND (e.estado_eliminado = false));


--
-- Name: aggregatedcounter id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.aggregatedcounter ALTER COLUMN id SET DEFAULT nextval('hangfire.aggregatedcounter_id_seq'::regclass);


--
-- Name: counter id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.counter ALTER COLUMN id SET DEFAULT nextval('hangfire.counter_id_seq'::regclass);


--
-- Name: hash id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.hash ALTER COLUMN id SET DEFAULT nextval('hangfire.hash_id_seq'::regclass);


--
-- Name: job id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.job ALTER COLUMN id SET DEFAULT nextval('hangfire.job_id_seq'::regclass);


--
-- Name: jobparameter id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.jobparameter ALTER COLUMN id SET DEFAULT nextval('hangfire.jobparameter_id_seq'::regclass);


--
-- Name: jobqueue id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.jobqueue ALTER COLUMN id SET DEFAULT nextval('hangfire.jobqueue_id_seq'::regclass);


--
-- Name: list id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.list ALTER COLUMN id SET DEFAULT nextval('hangfire.list_id_seq'::regclass);


--
-- Name: set id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.set ALTER COLUMN id SET DEFAULT nextval('hangfire.set_id_seq'::regclass);


--
-- Name: state id; Type: DEFAULT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.state ALTER COLUMN id SET DEFAULT nextval('hangfire.state_id_seq'::regclass);


--
-- Name: audit_logs id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs ALTER COLUMN id SET DEFAULT nextval('public.audit_logs_id_seq'::regclass);


--
-- Name: aggregatedcounter aggregatedcounter_key_key; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.aggregatedcounter
    ADD CONSTRAINT aggregatedcounter_key_key UNIQUE (key);


--
-- Name: aggregatedcounter aggregatedcounter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.aggregatedcounter
    ADD CONSTRAINT aggregatedcounter_pkey PRIMARY KEY (id);


--
-- Name: counter counter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.counter
    ADD CONSTRAINT counter_pkey PRIMARY KEY (id);


--
-- Name: hash hash_key_field_key; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.hash
    ADD CONSTRAINT hash_key_field_key UNIQUE (key, field);


--
-- Name: hash hash_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.hash
    ADD CONSTRAINT hash_pkey PRIMARY KEY (id);


--
-- Name: job job_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.job
    ADD CONSTRAINT job_pkey PRIMARY KEY (id);


--
-- Name: jobparameter jobparameter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.jobparameter
    ADD CONSTRAINT jobparameter_pkey PRIMARY KEY (id);


--
-- Name: jobqueue jobqueue_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.jobqueue
    ADD CONSTRAINT jobqueue_pkey PRIMARY KEY (id);


--
-- Name: list list_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.list
    ADD CONSTRAINT list_pkey PRIMARY KEY (id);


--
-- Name: lock lock_resource_key; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.lock
    ADD CONSTRAINT lock_resource_key UNIQUE (resource);

ALTER TABLE ONLY hangfire.lock REPLICA IDENTITY USING INDEX lock_resource_key;


--
-- Name: schema schema_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.schema
    ADD CONSTRAINT schema_pkey PRIMARY KEY (version);


--
-- Name: server server_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.server
    ADD CONSTRAINT server_pkey PRIMARY KEY (id);


--
-- Name: set set_key_value_key; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.set
    ADD CONSTRAINT set_key_value_key UNIQUE (key, value);


--
-- Name: set set_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.set
    ADD CONSTRAINT set_pkey PRIMARY KEY (id);


--
-- Name: state state_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.state
    ADD CONSTRAINT state_pkey PRIMARY KEY (id);


--
-- Name: accesorios Accesorio_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accesorios
    ADD CONSTRAINT "Accesorio_pk" PRIMARY KEY (id_accesorio);


--
-- Name: categorias Categoria_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT "Categoria_pk" PRIMARY KEY (id_categoria);


--
-- Name: componentes Componente_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.componentes
    ADD CONSTRAINT "Componente_pk" PRIMARY KEY (id_componente);


--
-- Name: empresas_mantenimiento Empresa_Mantenimiento_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.empresas_mantenimiento
    ADD CONSTRAINT "Empresa_Mantenimiento_pk" PRIMARY KEY (id_empresa_mantenimiento);


--
-- Name: equipos Equipo_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.equipos
    ADD CONSTRAINT "Equipo_pk" PRIMARY KEY (id_equipo);


--
-- Name: gaveteros Gavetero_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.gaveteros
    ADD CONSTRAINT "Gavetero_pk" PRIMARY KEY (id_gavetero);


--
-- Name: grupos_equipos Grupo_Equipo_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.grupos_equipos
    ADD CONSTRAINT "Grupo_Equipo_pk" PRIMARY KEY (id_grupo_equipo);


--
-- Name: mantenimientos Mantenimiento_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mantenimientos
    ADD CONSTRAINT "Mantenimiento_pk" PRIMARY KEY (id_mantenimiento);


--
-- Name: muebles Mueble_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.muebles
    ADD CONSTRAINT "Mueble_pk" PRIMARY KEY (id_mueble);


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: prestamos Prestamo_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.prestamos
    ADD CONSTRAINT "Prestamo_pk" PRIMARY KEY (id_prestamo);


--
-- Name: usuarios Usuario_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT "Usuario_pk" PRIMARY KEY (carnet);


--
-- Name: ambientes ambientes_nombre_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ambientes
    ADD CONSTRAINT ambientes_nombre_key UNIQUE (nombre);


--
-- Name: ambientes ambientes_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ambientes
    ADD CONSTRAINT ambientes_pkey PRIMARY KEY (id_ambiente);


--
-- Name: audit_logs audit_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_pkey PRIMARY KEY (id);


--
-- Name: avisos_disponibilidad avisos_disponibilidad_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.avisos_disponibilidad
    ADD CONSTRAINT avisos_disponibilidad_pkey PRIMARY KEY (id_aviso);


--
-- Name: carreras carrera_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.carreras
    ADD CONSTRAINT carrera_pkey PRIMARY KEY (id_carrera);


--
-- Name: codigos_autenticacion codigos_autenticacion_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.codigos_autenticacion
    ADD CONSTRAINT codigos_autenticacion_pkey PRIMARY KEY (hash);


--
-- Name: comentarios_equipos comentarios_equipos_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.comentarios_equipos
    ADD CONSTRAINT comentarios_equipos_pkey PRIMARY KEY (id_comentario_equipo);


--
-- Name: contratos contrato_id; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.contratos
    ADD CONSTRAINT contrato_id PRIMARY KEY (id);


--
-- Name: detalles_mantenimientos detalles_mantenimientos_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.detalles_mantenimientos
    ADD CONSTRAINT detalles_mantenimientos_pkey PRIMARY KEY (id_detalle_mantenimiento);


--
-- Name: detalles_prestamos detalles_prestamos_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.detalles_prestamos
    ADD CONSTRAINT detalles_prestamos_pkey PRIMARY KEY (id_detalle_prestamo);


--
-- Name: notificaciones notificaciones_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notificaciones
    ADD CONSTRAINT notificaciones_pkey PRIMARY KEY (id_notificacion);


--
-- Name: configuraciones_sistema pk_configuraciones_sistema; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.configuraciones_sistema
    ADD CONSTRAINT pk_configuraciones_sistema PRIMARY KEY (id_configuracion);


--
-- Name: procedencias procedencias_nombre_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.procedencias
    ADD CONSTRAINT procedencias_nombre_key UNIQUE (nombre);


--
-- Name: procedencias procedencias_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.procedencias
    ADD CONSTRAINT procedencias_pkey PRIMARY KEY (id_procedencia);


--
-- Name: usuarios unique_carnet; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT unique_carnet UNIQUE (carnet);


--
-- Name: carreras unique_carreras; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.carreras
    ADD CONSTRAINT unique_carreras UNIQUE (nombre);


--
-- Name: categorias unique_categorias; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT unique_categorias UNIQUE (nombre);


--
-- Name: equipos unique_codigo_imt; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.equipos
    ADD CONSTRAINT unique_codigo_imt UNIQUE (codigo_imt);


--
-- Name: usuarios unique_email; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT unique_email UNIQUE (email);


--
-- Name: grupos_equipos unique_grupos_equipos_nombre_modelo_marca; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.grupos_equipos
    ADD CONSTRAINT unique_grupos_equipos_nombre_modelo_marca UNIQUE (nombre, modelo, marca);


--
-- Name: muebles unique_nombre; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.muebles
    ADD CONSTRAINT unique_nombre UNIQUE (nombre);


--
-- Name: empresas_mantenimiento unique_nombre_empresas_mantenimiento; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.empresas_mantenimiento
    ADD CONSTRAINT unique_nombre_empresas_mantenimiento UNIQUE (nombre);


--
-- Name: gaveteros unique_nombre_gaveteros; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.gaveteros
    ADD CONSTRAINT unique_nombre_gaveteros UNIQUE (nombre);


--
-- Name: ix_hangfire_counter_expireat; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_counter_expireat ON hangfire.counter USING btree (expireat);


--
-- Name: ix_hangfire_counter_key; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_counter_key ON hangfire.counter USING btree (key);


--
-- Name: ix_hangfire_hash_expireat; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_hash_expireat ON hangfire.hash USING btree (expireat);


--
-- Name: ix_hangfire_job_expireat; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_job_expireat ON hangfire.job USING btree (expireat);


--
-- Name: ix_hangfire_job_statename; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_job_statename ON hangfire.job USING btree (statename);


--
-- Name: ix_hangfire_job_statename_is_not_null; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_job_statename_is_not_null ON hangfire.job USING btree (statename) INCLUDE (id) WHERE (statename IS NOT NULL);


--
-- Name: ix_hangfire_jobparameter_jobidandname; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_jobparameter_jobidandname ON hangfire.jobparameter USING btree (jobid, name);


--
-- Name: ix_hangfire_jobqueue_fetchedat_queue_jobid; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_jobqueue_fetchedat_queue_jobid ON hangfire.jobqueue USING btree (fetchedat NULLS FIRST, queue, jobid);


--
-- Name: ix_hangfire_jobqueue_jobidandqueue; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_jobqueue_jobidandqueue ON hangfire.jobqueue USING btree (jobid, queue);


--
-- Name: ix_hangfire_jobqueue_queueandfetchedat; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_jobqueue_queueandfetchedat ON hangfire.jobqueue USING btree (queue, fetchedat);


--
-- Name: ix_hangfire_list_expireat; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_list_expireat ON hangfire.list USING btree (expireat);


--
-- Name: ix_hangfire_set_expireat; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_set_expireat ON hangfire.set USING btree (expireat);


--
-- Name: ix_hangfire_set_key_score; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_set_key_score ON hangfire.set USING btree (key, score);


--
-- Name: ix_hangfire_state_jobid; Type: INDEX; Schema: hangfire; Owner: -
--

CREATE INDEX ix_hangfire_state_jobid ON hangfire.state USING btree (jobid);


--
-- Name: idx_accesorios_identificadores; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_accesorios_identificadores ON public.accesorios USING btree (nombre, id_equipo, estado_eliminado);


--
-- Name: idx_audit_admin; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_admin ON public.audit_logs USING btree (admin_carnet, "timestamp" DESC, estado_eliminado);


--
-- Name: idx_audit_entidad; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_entidad ON public.audit_logs USING btree (entidad, entidad_id, estado_eliminado);


--
-- Name: idx_audit_entidad_accion_fecha; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_entidad_accion_fecha ON public.audit_logs USING btree (entidad, accion, "timestamp" DESC, estado_eliminado);


--
-- Name: idx_carreras_nombre; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_carreras_nombre ON public.carreras USING btree (nombre, estado_eliminado);


--
-- Name: idx_categorias_nombre; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_categorias_nombre ON public.categorias USING btree (nombre, estado_eliminado);


--
-- Name: idx_componentes; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_componentes ON public.componentes USING btree (nombre, id_equipo, estado_eliminado);


--
-- Name: idx_detalles_mantenimientos; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_detalles_mantenimientos ON public.detalles_mantenimientos USING btree (id_mantenimiento, estado_eliminado);


--
-- Name: idx_detalles_prestamos; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_detalles_prestamos ON public.detalles_prestamos USING btree (id_prestamo, estado_eliminado);


--
-- Name: idx_empresas_mantenimiento; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_empresas_mantenimiento ON public.empresas_mantenimiento USING btree (nombre, estado_eliminado);


--
-- Name: idx_equipos_identificadores; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_equipos_identificadores ON public.equipos USING btree (id_grupo_equipo, codigo_imt, estado_eliminado);


--
-- Name: idx_gaveteros_identificadores; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_gaveteros_identificadores ON public.gaveteros USING btree (nombre, id_mueble, estado_eliminado);


--
-- Name: idx_grupos_equipos_identificadores; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_grupos_equipos_identificadores ON public.grupos_equipos USING btree (id_categoria, nombre, modelo, marca, estado_eliminado);


--
-- Name: idx_mantenimientos_fecha_empresa; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mantenimientos_fecha_empresa ON public.mantenimientos USING btree (fecha_mantenimiento, fecha_final_mantenimiento, id_empresa, estado_eliminado);


--
-- Name: idx_muebles_nombre; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_muebles_nombre ON public.usuarios USING btree (nombre, estado_eliminado);


--
-- Name: idx_prestamos_fechas; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_prestamos_fechas ON public.prestamos USING btree (fecha_prestamo_esperada, fecha_devolucion_esperada, carnet, estado_eliminado);


--
-- Name: idx_usuarios_email; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_usuarios_email ON public.usuarios USING btree (email, estado_eliminado);


--
-- Name: ix_ambientes_carnet_administrador; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_ambientes_carnet_administrador ON public.ambientes USING btree (carnet_administrador);


--
-- Name: ix_avisos_disponibilidad_pendiente; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_avisos_disponibilidad_pendiente ON public.avisos_disponibilidad USING btree (notificado, estado_eliminado);


--
-- Name: ix_codigos_autenticacion_expira_usado; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_codigos_autenticacion_expira_usado ON public.codigos_autenticacion USING btree (expira, usado);


--
-- Name: ix_comentarios_equipos_grupo_fecha; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_comentarios_equipos_grupo_fecha ON public.comentarios_equipos USING btree (id_grupo_equipo, fecha_creacion, estado_eliminado);


--
-- Name: ix_comentarios_equipos_padre_estado; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_comentarios_equipos_padre_estado ON public.comentarios_equipos USING btree (id_comentario_padre, estado_eliminado);


--
-- Name: ix_detalles_prestamos_id_equipo; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_detalles_prestamos_id_equipo ON public.detalles_prestamos USING btree (id_equipo);


--
-- Name: ix_equipos_id_ambiente; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_equipos_id_ambiente ON public.equipos USING btree (id_ambiente);


--
-- Name: ix_equipos_id_procedencia; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_equipos_id_procedencia ON public.equipos USING btree (id_procedencia);


--
-- Name: ix_muebles_id_ambiente; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_muebles_id_ambiente ON public.muebles USING btree (id_ambiente);


--
-- Name: ix_notificaciones_carnet; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_notificaciones_carnet ON public.notificaciones USING btree (carnet_usuario, leido, estado_eliminado);


--
-- Name: ix_prestamos_carnet_estado; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_prestamos_carnet_estado ON public.prestamos USING btree (carnet, estado_prestamo, estado_eliminado);


--
-- Name: ix_prestamos_guardados_usuario; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_prestamos_guardados_usuario ON public.prestamos USING btree (carnet, fecha_solicitud DESC) WHERE ((guardado = true) AND (estado_eliminado = false));


--
-- Name: ix_usuarios_google_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_usuarios_google_id ON public.usuarios USING btree (google_id) WHERE (google_id IS NOT NULL);


--
-- Name: ix_usuarios_refresh_token; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_usuarios_refresh_token ON public.usuarios USING btree (refresh_token);


--
-- Name: ix_usuarios_token_verificacion; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_usuarios_token_verificacion ON public.usuarios USING btree (token_verificacion_hash) WHERE (token_verificacion_hash IS NOT NULL);


--
-- Name: unique_codigo_ucb; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX unique_codigo_ucb ON public.equipos USING btree (codigo_ucb);


--
-- Name: equipos trg_actualizar_costo_promedio_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_actualizar_costo_promedio_delete AFTER DELETE ON public.equipos FOR EACH ROW EXECUTE FUNCTION public.fn_actualizar_costo_promedio_grupo();


--
-- Name: equipos trg_actualizar_costo_promedio_insert; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_actualizar_costo_promedio_insert AFTER INSERT ON public.equipos FOR EACH ROW EXECUTE FUNCTION public.fn_actualizar_costo_promedio_grupo();


--
-- Name: equipos trg_actualizar_costo_promedio_update; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_actualizar_costo_promedio_update AFTER UPDATE OF costo_referencia, estado_eliminado, estado_equipo ON public.equipos FOR EACH ROW EXECUTE FUNCTION public.fn_actualizar_costo_promedio_grupo();


--
-- Name: equipos trg_equipo_estado_actualiza_grupo; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_equipo_estado_actualiza_grupo AFTER UPDATE OF estado_eliminado ON public.equipos FOR EACH ROW EXECUTE FUNCTION public.fn_actualizar_cantidad_equipo_por_estado();


--
-- Name: equipos trg_equipos_after_insert; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_equipos_after_insert AFTER INSERT ON public.equipos FOR EACH ROW EXECUTE FUNCTION public.fn_incrementar_cantidad_equipos();


--
-- Name: gaveteros trg_gavetero_movimiento_actualiza_numero_mueble; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_gavetero_movimiento_actualiza_numero_mueble AFTER UPDATE ON public.gaveteros FOR EACH ROW WHEN ((old.id_mueble IS DISTINCT FROM new.id_mueble)) EXECUTE FUNCTION public.fn_actualizar_gavetero_tras_update_mueble();


--
-- Name: gaveteros trg_gaveteros_estado_conteo_mueble; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_gaveteros_estado_conteo_mueble AFTER UPDATE OF estado_eliminado ON public.gaveteros FOR EACH ROW EXECUTE FUNCTION public.fn_actualizar_conteo_gaveteros_por_estado();


--
-- Name: gaveteros trg_incrementar_gaveteros; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_incrementar_gaveteros AFTER INSERT ON public.gaveteros FOR EACH ROW EXECUTE FUNCTION public.fn_incrementar_numero_gaveteros();


--
-- Name: mantenimientos trg_mantenimientos_cascade_estado_a_detalles; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_mantenimientos_cascade_estado_a_detalles AFTER UPDATE OF estado_eliminado ON public.mantenimientos FOR EACH ROW EXECUTE FUNCTION public.fn_estado_eliminado_mantenimiento_a_detalle();


--
-- Name: prestamos trg_prestamos_estado_a_detalles; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_prestamos_estado_a_detalles AFTER UPDATE OF estado_eliminado ON public.prestamos FOR EACH ROW EXECUTE FUNCTION public.fn_estado_eliminado_prestamo_a_detalle();


--
-- Name: equipos trg_update_cantidad_tras_update_equipos; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_update_cantidad_tras_update_equipos AFTER UPDATE ON public.equipos FOR EACH ROW WHEN ((old.id_grupo_equipo IS DISTINCT FROM new.id_grupo_equipo)) EXECUTE FUNCTION public.fn_actualizar_cantidad_tras_update_equipos();


--
-- Name: jobparameter jobparameter_jobid_fkey; Type: FK CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.jobparameter
    ADD CONSTRAINT jobparameter_jobid_fkey FOREIGN KEY (jobid) REFERENCES hangfire.job(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: state state_jobid_fkey; Type: FK CONSTRAINT; Schema: hangfire; Owner: -
--

ALTER TABLE ONLY hangfire.state
    ADD CONSTRAINT state_jobid_fkey FOREIGN KEY (jobid) REFERENCES hangfire.job(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: accesorios Accesorio_Equipo_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accesorios
    ADD CONSTRAINT "Accesorio_Equipo_fk" FOREIGN KEY (id_equipo) REFERENCES public.equipos(id_equipo);


--
-- Name: componentes Componente_Equipo_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.componentes
    ADD CONSTRAINT "Componente_Equipo_fk" FOREIGN KEY (id_equipo) REFERENCES public.equipos(id_equipo);


--
-- Name: equipos Equipo_Gavetero_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.equipos
    ADD CONSTRAINT "Equipo_Gavetero_fk" FOREIGN KEY (id_gavetero) REFERENCES public.gaveteros(id_gavetero);


--
-- Name: equipos Equipo_Grupo_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.equipos
    ADD CONSTRAINT "Equipo_Grupo_fk" FOREIGN KEY (id_grupo_equipo) REFERENCES public.grupos_equipos(id_grupo_equipo);


--
-- Name: grupos_equipos Grupo_Categoria_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.grupos_equipos
    ADD CONSTRAINT "Grupo_Categoria_fk" FOREIGN KEY (id_categoria) REFERENCES public.categorias(id_categoria);


--
-- Name: mantenimientos Mantenimiento_Empresa_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mantenimientos
    ADD CONSTRAINT "Mantenimiento_Empresa_fk" FOREIGN KEY (id_empresa) REFERENCES public.empresas_mantenimiento(id_empresa_mantenimiento);


--
-- Name: prestamos Prestamo_Carrera_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.prestamos
    ADD CONSTRAINT "Prestamo_Carrera_fk" FOREIGN KEY (id_carrera) REFERENCES public.carreras(id_carrera);


--
-- Name: prestamos Prestamo_Usuario_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.prestamos
    ADD CONSTRAINT "Prestamo_Usuario_fk" FOREIGN KEY (carnet) REFERENCES public.usuarios(carnet);


--
-- Name: prestamos Prestamo_contrato_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.prestamos
    ADD CONSTRAINT "Prestamo_contrato_fk" FOREIGN KEY (id_contrato) REFERENCES public.contratos(id) NOT VALID;


--
-- Name: avisos_disponibilidad avisos_disponibilidad_carnet_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.avisos_disponibilidad
    ADD CONSTRAINT avisos_disponibilidad_carnet_usuario_fkey FOREIGN KEY (carnet_usuario) REFERENCES public.usuarios(carnet);


--
-- Name: avisos_disponibilidad avisos_disponibilidad_id_grupo_equipo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.avisos_disponibilidad
    ADD CONSTRAINT avisos_disponibilidad_id_grupo_equipo_fkey FOREIGN KEY (id_grupo_equipo) REFERENCES public.grupos_equipos(id_grupo_equipo);


--
-- Name: comentarios_equipos comentarios_equipos_carnet_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.comentarios_equipos
    ADD CONSTRAINT comentarios_equipos_carnet_usuario_fkey FOREIGN KEY (carnet_usuario) REFERENCES public.usuarios(carnet);


--
-- Name: comentarios_equipos comentarios_equipos_id_comentario_padre_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.comentarios_equipos
    ADD CONSTRAINT comentarios_equipos_id_comentario_padre_fkey FOREIGN KEY (id_comentario_padre) REFERENCES public.comentarios_equipos(id_comentario_equipo) ON DELETE RESTRICT;


--
-- Name: comentarios_equipos comentarios_equipos_id_grupo_equipo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.comentarios_equipos
    ADD CONSTRAINT comentarios_equipos_id_grupo_equipo_fkey FOREIGN KEY (id_grupo_equipo) REFERENCES public.grupos_equipos(id_grupo_equipo);


--
-- Name: configuraciones_sistema configuraciones_sistema_carnet_jefe_carrera_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.configuraciones_sistema
    ADD CONSTRAINT configuraciones_sistema_carnet_jefe_carrera_fkey FOREIGN KEY (carnet_jefe_carrera) REFERENCES public.usuarios(carnet) ON DELETE SET NULL;


--
-- Name: detalles_prestamos detalles_prestamos_id_grupo_equipo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.detalles_prestamos
    ADD CONSTRAINT detalles_prestamos_id_grupo_equipo_fkey FOREIGN KEY (id_grupo_equipo) REFERENCES public.grupos_equipos(id_grupo_equipo);


--
-- Name: equipos equipos_id_ambiente_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.equipos
    ADD CONSTRAINT equipos_id_ambiente_fkey FOREIGN KEY (id_ambiente) REFERENCES public.ambientes(id_ambiente) ON DELETE RESTRICT;


--
-- Name: equipos equipos_id_procedencia_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.equipos
    ADD CONSTRAINT equipos_id_procedencia_fkey FOREIGN KEY (id_procedencia) REFERENCES public.procedencias(id_procedencia) ON DELETE RESTRICT;


--
-- Name: ambientes fk_ambientes_administrador; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ambientes
    ADD CONSTRAINT fk_ambientes_administrador FOREIGN KEY (carnet_administrador) REFERENCES public.usuarios(carnet) ON DELETE RESTRICT;


--
-- Name: detalles_mantenimientos fk_detalle_mantenimiento_equipo; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.detalles_mantenimientos
    ADD CONSTRAINT fk_detalle_mantenimiento_equipo FOREIGN KEY (id_equipo) REFERENCES public.equipos(id_equipo);


--
-- Name: detalles_mantenimientos fk_detalles_mantenimiento; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.detalles_mantenimientos
    ADD CONSTRAINT fk_detalles_mantenimiento FOREIGN KEY (id_mantenimiento) REFERENCES public.mantenimientos(id_mantenimiento);


--
-- Name: detalles_prestamos fk_equipo; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.detalles_prestamos
    ADD CONSTRAINT fk_equipo FOREIGN KEY (id_equipo) REFERENCES public.equipos(id_equipo);


--
-- Name: gaveteros fk_gaveteros_muebles; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.gaveteros
    ADD CONSTRAINT fk_gaveteros_muebles FOREIGN KEY (id_mueble) REFERENCES public.muebles(id_mueble);


--
-- Name: muebles fk_muebles_ambientes; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.muebles
    ADD CONSTRAINT fk_muebles_ambientes FOREIGN KEY (id_ambiente) REFERENCES public.ambientes(id_ambiente) ON DELETE RESTRICT;


--
-- Name: detalles_prestamos fk_prestamo; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.detalles_prestamos
    ADD CONSTRAINT fk_prestamo FOREIGN KEY (id_prestamo) REFERENCES public.prestamos(id_prestamo);


--
-- Name: usuarios fk_usuarios_carrera; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT fk_usuarios_carrera FOREIGN KEY (id_carrera) REFERENCES public.carreras(id_carrera);


--
-- Name: notificaciones notificaciones_carnet_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notificaciones
    ADD CONSTRAINT notificaciones_carnet_usuario_fkey FOREIGN KEY (carnet_usuario) REFERENCES public.usuarios(carnet);


--
-- PostgreSQL database dump complete
--

