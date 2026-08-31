create sequence "Accesorio_Id_Accesorio_seq";

alter sequence "Accesorio_Id_Accesorio_seq" owner to postgres;

create sequence "Categoria_ID_Categoria_seq";

alter sequence "Categoria_ID_Categoria_seq" owner to postgres;

create sequence "Componente_Id_Componente_seq";

alter sequence "Componente_Id_Componente_seq" owner to postgres;

create sequence "Empresa_Mantenimiento_Id_Empresa_seq";

alter sequence "Empresa_Mantenimiento_Id_Empresa_seq" owner to postgres;

create sequence "Equipo_Id_equipo_seq";

alter sequence "Equipo_Id_equipo_seq" owner to postgres;

create sequence "Gavetero_Id_Gavetero_seq";

alter sequence "Gavetero_Id_Gavetero_seq" owner to postgres;

create sequence "Grupo_Equipo_Id_Grupo_equipo_seq";

alter sequence "Grupo_Equipo_Id_Grupo_equipo_seq" owner to postgres;

create sequence "Mantenimiento_Id_Mantenimiento_seq";

alter sequence "Mantenimiento_Id_Mantenimiento_seq" owner to postgres;

create sequence "Mueble_Id_Mueble_seq";

alter sequence "Mueble_Id_Mueble_seq" owner to postgres;

create sequence "Prestamo_Id_Prestamo_seq";

alter sequence "Prestamo_Id_Prestamo_seq" owner to postgres;

create sequence carrera_id_carrera_seq
    as integer;

alter sequence carrera_id_carrera_seq owner to postgres;

create sequence detalles_mantenimientos_id_detalle_mantenimiento_seq
    as integer;

alter sequence detalles_mantenimientos_id_detalle_mantenimiento_seq owner to postgres;

create sequence detalles_mantenimientos_id_detalle_mantenimiento_seq1;

alter sequence detalles_mantenimientos_id_detalle_mantenimiento_seq1 owner to postgres;

create sequence nombre_de_tu_tabla_id_seq;

alter sequence nombre_de_tu_tabla_id_seq owner to postgres;

create type estado_disponibilidad as enum ('disponible', 'mantenimiento', 'ocupado');

alter type estado_disponibilidad owner to postgres;

create type estado_equipo as enum ('operativo', 'parcialmente_operativo', 'inoperativo');

alter type estado_equipo owner to postgres;

create type estado_prestamo as enum ('pendiente', 'rechazado', 'aprobado', 'activo', 'finalizado', 'cancelado', 'atrasado');

alter type estado_prestamo owner to postgres;

create type tipo_mantenimiento as enum ('correctivo', 'preventivo');

alter type tipo_mantenimiento owner to postgres;

create type tipo_usuario as enum ('docente', 'administrador', 'estudiante', 'administrativo', 'administrador_laboratorio');

alter type tipo_usuario owner to postgres;

create table categorias
(
    id_categoria     integer generated always as identity
        constraint "Categoria_pk"
            primary key,
    nombre           varchar(255)          not null
        constraint unique_categorias
            unique,
    estado_eliminado boolean default false not null
);

alter table categorias
    owner to postgres;

alter sequence "Categoria_ID_Categoria_seq" owned by categorias.id_categoria;

create index idx_categorias_nombre
    on categorias (nombre, estado_eliminado);

create table empresas_mantenimiento
(
    id_empresa_mantenimiento integer generated always as identity
        constraint "Empresa_Mantenimiento_pk"
            primary key,
    nombre                   varchar(255)          not null
        constraint unique_nombre_empresas_mantenimiento
            unique,
    direccion                varchar(512),
    telefono                 varchar(64),
    nit                      varchar(255),
    estado_eliminado         boolean default false not null,
    nombre_responsable       varchar(64),
    apellido_responsable     varchar(64)
);

comment on column empresas_mantenimiento.id_empresa_mantenimiento is 'Código empresa';

alter table empresas_mantenimiento
    owner to postgres;

alter sequence "Empresa_Mantenimiento_Id_Empresa_seq" owned by empresas_mantenimiento.id_empresa_mantenimiento;

create index idx_empresas_mantenimiento
    on empresas_mantenimiento (nombre, estado_eliminado);

create table grupos_equipos
(
    id_grupo_equipo          integer generated always as identity
        constraint "Grupo_Equipo_pk"
            primary key,
    nombre                   varchar(256)                 not null,
    modelo                   varchar(512)                 not null,
    url_data_sheet           text,
    cantidad                 integer        default 0     not null,
    marca                    varchar(256)                 not null,
    id_categoria             integer                      not null
        constraint "Grupo_Categoria_fk"
            references categorias,
    estado_eliminado         boolean        default false not null,
    url_imagen               text                         not null,
    descripcion              text                         not null,
    costo_promedio           numeric(10, 2) default 0,
    tiempo_max_prestamo_dias integer        default 7     not null
        constraint ck_grupos_equipos_tiempo_max_prestamo
            check ((tiempo_max_prestamo_dias >= 1) AND (tiempo_max_prestamo_dias <= 365)),
    constraint unique_grupos_equipos_nombre_modelo_marca
        unique (nombre, modelo, marca)
);

comment on column grupos_equipos.descripcion is 'Esto se mostrar en la pagina web';

alter table grupos_equipos
    owner to postgres;

alter sequence "Grupo_Equipo_Id_Grupo_equipo_seq" owned by grupos_equipos.id_grupo_equipo;

create index idx_grupos_equipos_identificadores
    on grupos_equipos (id_categoria, nombre, modelo, marca, estado_eliminado);

create table mantenimientos
(
    id_mantenimiento          integer generated always as identity
        constraint "Mantenimiento_pk"
            primary key,
    descripcion               text,
    costo                     double precision,
    fecha_mantenimiento       date                  not null,
    id_empresa                integer               not null
        constraint "Mantenimiento_Empresa_fk"
            references empresas_mantenimiento,
    estado_eliminado          boolean default false not null,
    fecha_final_mantenimiento date                  not null
);

alter table mantenimientos
    owner to postgres;

alter sequence "Mantenimiento_Id_Mantenimiento_seq" owned by mantenimientos.id_mantenimiento;

create index idx_mantenimientos_fecha_empresa
    on mantenimientos (fecha_mantenimiento, fecha_final_mantenimiento, id_empresa, estado_eliminado);

create table muebles
(
    id_mueble        integer generated always as identity
        constraint "Mueble_pk"
            primary key,
    nombre           varchar(255)          not null
        constraint unique_nombre
            unique,
    tipo             varchar(255),
    ubicacion        varchar(255),
    numero_gaveteros integer default 0     not null,
    estado_eliminado boolean default false not null,
    longitud         double precision,
    profundidad      double precision,
    altura           double precision,
    costo            double precision
);

comment on column muebles.id_mueble is 'Código del mueble';

alter table muebles
    owner to postgres;

alter sequence "Mueble_Id_Mueble_seq" owned by muebles.id_mueble;

create table gaveteros
(
    id_gavetero      integer generated always as identity
        constraint "Gavetero_pk"
            primary key,
    nombre           varchar(255)          not null
        constraint unique_nombre_gaveteros
            unique,
    tipo             varchar(255),
    estado_eliminado boolean default false not null,
    id_mueble        integer               not null
        constraint fk_gaveteros_muebles
            references muebles,
    longitud         double precision,
    profundidad      double precision,
    altura           double precision
);

alter table gaveteros
    owner to postgres;

alter sequence "Gavetero_Id_Gavetero_seq" owned by gaveteros.id_gavetero;

create table ambientes
(
    id_ambiente integer generated by default as identity primary key,
    nombre varchar(255) not null unique,
    estado_eliminado boolean not null default false
);

create table procedencias
(
    id_procedencia integer generated by default as identity primary key,
    nombre varchar(255) not null unique,
    estado_eliminado boolean not null default false
);

insert into procedencias(nombre) values ('Compra'), ('Donación'), ('Préstamo');

create table equipos
(
    id_equipo            integer generated always as identity
        constraint "Equipo_pk"
            primary key,
    id_grupo_equipo      integer                                             not null
        constraint "Equipo_Grupo_fk"
            references grupos_equipos,
    codigo_imt           integer                                             not null
        constraint unique_codigo_imt
            unique,
    descripcion          text,
    estado_equipo        estado_equipo    default 'operativo'::estado_equipo not null,
    numero_serial        varchar(255),
    ubicacion            varchar(255),
    id_ambiente          integer references ambientes(id_ambiente) on delete restrict,
    id_procedencia       integer references procedencias(id_procedencia) on delete restrict,
    costo_referencia     double precision default 0,
    tiempo_max_prestamo  integer          default 9999,
    procedencia          varchar(255),
    id_gavetero          integer
        constraint "Equipo_Gavetero_fk"
            references gaveteros,
    estado_eliminado     boolean          default false                      not null,
    fecha_ingreso_equipo date             default CURRENT_DATE               not null,
    codigo_ucb           varchar
);

alter table equipos
    owner to postgres;

create index ix_equipos_id_ambiente on equipos(id_ambiente);
create index ix_equipos_id_procedencia on equipos(id_procedencia);

alter sequence "Equipo_Id_equipo_seq" owned by equipos.id_equipo;

create table accesorios
(
    id_accesorio     integer generated always as identity
        constraint "Accesorio_pk"
            primary key,
    nombre           varchar(255)          not null,
    descripcion      text,
    modelo           varchar(255)          not null,
    url_data_sheet   text,
    precio           double precision,
    id_equipo        integer               not null
        constraint "Accesorio_Equipo_fk"
            references equipos,
    tipo             varchar(255),
    estado_eliminado boolean default false not null
);

comment on column accesorios.id_accesorio is 'Código del accesorio';

alter table accesorios
    owner to postgres;

alter sequence "Accesorio_Id_Accesorio_seq" owned by accesorios.id_accesorio;

create index idx_accesorios_identificadores
    on accesorios (nombre, id_equipo, estado_eliminado);

create table componentes
(
    id_componente     integer generated always as identity
        constraint "Componente_pk"
            primary key,
    descripcion       text,
    modelo            varchar(255)          not null,
    url_data_sheet    text,
    tipo              varchar(255),
    precio_referencia double precision,
    nombre            varchar(255)          not null,
    id_equipo         integer               not null
        constraint "Componente_Equipo_fk"
            references equipos,
    estado_eliminado  boolean default false not null
);

comment on column componentes.id_componente is 'Código del componente';

alter table componentes
    owner to postgres;

alter sequence "Componente_Id_Componente_seq" owned by componentes.id_componente;

create index idx_componentes
    on componentes (nombre, id_equipo, estado_eliminado);

create index idx_equipos_identificadores
    on equipos (id_grupo_equipo, codigo_imt, estado_eliminado);

create unique index unique_codigo_ucb
    on equipos (codigo_ucb);

create index idx_gaveteros_identificadores
    on gaveteros (nombre, id_mueble, estado_eliminado);

create table carreras
(
    id_carrera       integer generated always as identity
        constraint carrera_pkey
            primary key,
    nombre           varchar(255)          not null
        constraint unique_carreras
            unique,
    estado_eliminado boolean default false not null
);

alter table carreras
    owner to postgres;

create index idx_carreras_nombre
    on carreras (nombre, estado_eliminado);

create table contratos
(
    id       integer generated always as identity
        constraint contrato_id
            primary key,
    contrato text
);

alter table contratos
    owner to postgres;

alter sequence nombre_de_tu_tabla_id_seq owned by contratos.id;

create table detalles_mantenimientos
(
    id_detalle_mantenimiento integer generated always as identity
        primary key,
    id_mantenimiento         integer               not null
        constraint fk_detalles_mantenimiento
            references mantenimientos,
    descripcion              text,
    id_equipo                integer               not null
        constraint fk_detalle_mantenimiento_equipo
            references equipos,
    estado_eliminado         boolean default false not null,
    tipo_mantenimiento       varchar
);

alter table detalles_mantenimientos
    owner to postgres;

alter sequence detalles_mantenimientos_id_detalle_mantenimiento_seq1 owned by detalles_mantenimientos.id_detalle_mantenimiento;

create index idx_detalles_mantenimientos
    on detalles_mantenimientos (id_mantenimiento, estado_eliminado);

create table usuarios
(
    carnet               varchar(64)                                     not null
        constraint "Usuario_pk"
            primary key
        constraint unique_carnet
            unique,
    nombre               varchar(64)                                     not null,
    apellido_paterno     varchar(64)                                     not null,
    apellido_materno     varchar(64)                                     not null,
    rol                  tipo_usuario default 'estudiante'::tipo_usuario not null,
    contrasena           text                                            not null,
    email                varchar(255)                                    not null
        constraint unique_email
            unique,
    telefono             varchar(32)                                     not null,
    telefono_referencia  varchar(32),
    nombre_referencia    varchar(32),
    email_referencia     varchar(255),
    estado_eliminado     boolean      default false                      not null,
    id_carrera           integer                                         not null
        constraint fk_usuarios_carrera
            references carreras,
    imagen_frente_carnet bytea,
    imagen_atras_carnet  bytea,
    refresh_token        text,
    refresh_token_expiry timestamp with time zone,
    bloqueado            boolean      default false                      not null,
    motivo_bloqueo       text
);

alter table usuarios
    owner to postgres;

create table prestamos
(
    autorizado_por           varchar(255),
    entregado_por            varchar(255),
    motivo_rechazo           varchar(1024),
    id_prestamo               integer generated always as identity
        constraint "Prestamo_pk"
            primary key,
    fecha_solicitud           timestamp       default (now() AT TIME ZONE 'America/La_Paz'::text) not null,
    fecha_prestamo            timestamp,
    fecha_devolucion_esperada timestamp                                                           not null,
    observacion               text,
    estado_prestamo           estado_prestamo default 'pendiente'::estado_prestamo                not null,
    carnet                    varchar(64)                                                         not null
        constraint "Prestamo_Usuario_fk"
            references usuarios,
    estado_eliminado          boolean         default false                                       not null,
    fecha_devolucion          timestamp,
    fecha_prestamo_esperada   timestamp                                                           not null,
    id_contrato               integer
        constraint "Prestamo_contrato_fk"
            references contratos,
    recordatorio_enviado      boolean         default false                                       not null,
    destino_prestamo          varchar(50)     default 'Universidad'::character varying            not null,
    id_carrera                integer
        constraint "Prestamo_Carrera_fk"
            references carreras,
    nombre_materia            varchar(255)
);

comment on column prestamos.id_prestamo is 'Código del préstamo';

alter table prestamos
    owner to postgres;

alter sequence "Prestamo_Id_Prestamo_seq" owned by prestamos.id_prestamo;

create index idx_prestamos_fechas
    on prestamos (fecha_prestamo_esperada, fecha_devolucion_esperada, carnet, estado_eliminado);

create index ix_prestamos_carnet_estado
    on prestamos (carnet, estado_prestamo, estado_eliminado);

create table detalles_prestamos
(
    id_detalle_prestamo   integer generated always as identity
        primary key,
    id_equipo             integer
        constraint fk_equipo
            references equipos,
    id_prestamo           integer               not null
        constraint fk_prestamo
            references prestamos,
    estado_eliminado      boolean default false not null,
    id_grupo_equipo       integer               not null
        references grupos_equipos,
    estado_equipo_retorno estado_equipo
);

alter table detalles_prestamos
    owner to postgres;

create index idx_detalles_prestamos
    on detalles_prestamos (id_prestamo, estado_eliminado);

create index ix_detalles_prestamos_id_equipo
    on detalles_prestamos (id_equipo);

create index idx_muebles_nombre
    on usuarios (nombre, estado_eliminado);

create index idx_usuarios_email
    on usuarios (email, estado_eliminado);

create index ix_usuarios_refresh_token
    on usuarios (refresh_token);

create table audit_logs
(
    id               serial
        primary key,
    admin_carnet     varchar(20)                            not null,
    admin_nombre     text                                   not null,
    accion           varchar(50)                            not null,
    entidad          varchar(100)                           not null,
    entidad_id       text,
    detalle          text,
    timestamp        timestamp with time zone default now() not null,
    estado_eliminado boolean                  default false not null
);

alter table audit_logs
    owner to postgres;

create index idx_audit_admin
    on audit_logs (admin_carnet asc, timestamp desc, estado_eliminado asc);

create index idx_audit_entidad
    on audit_logs (entidad, entidad_id, estado_eliminado);

create index idx_audit_entidad_accion_fecha
    on audit_logs (entidad asc, accion asc, timestamp desc, estado_eliminado asc);

create table notificaciones
(
    id_notificacion  integer generated always as identity
        primary key,
    carnet_usuario   varchar(20)                            not null
        references usuarios,
    tipo             varchar(50)                            not null,
    titulo           text                                   not null,
    contenido        text,
    detalle          text,
    leido            boolean                  default false not null,
    fecha_envio      timestamp with time zone default now() not null,
    estado_eliminado boolean                  default false not null
);

alter table notificaciones
    owner to postgres;

create index ix_notificaciones_carnet
    on notificaciones (carnet_usuario, leido, estado_eliminado);

create table avisos_disponibilidad
(
    id_aviso         integer generated always as identity
        primary key,
    carnet_usuario   varchar(20)                            not null
        references usuarios,
    id_grupo_equipo  integer                                not null
        references grupos_equipos,
    fecha            timestamp                              not null,
    cantidad         integer                  default 1     not null,
    notificado       boolean                  default false not null,
    fecha_creacion   timestamp with time zone default now() not null,
    estado_eliminado boolean                  default false not null
);

alter table avisos_disponibilidad
    owner to postgres;

create index ix_avisos_disponibilidad_pendiente
    on avisos_disponibilidad (notificado, estado_eliminado);

create table comentarios_equipos
(
    id_comentario_equipo integer generated always as identity
        primary key,
    id_grupo_equipo      integer                                   not null
        references grupos_equipos,
    carnet_usuario       varchar(20)                               not null
        references usuarios,
    contenido            varchar(1024)                             not null,
    fecha_creacion       timestamp with time zone default now()    not null,
    estado_eliminado     boolean                  default false    not null,
    id_comentario_padre  integer
        references comentarios_equipos
            on delete restrict,
    likes                integer                  default 0        not null,
    liked_by             text                     default ''::text not null
);

alter table comentarios_equipos
    owner to postgres;

create index ix_comentarios_equipos_grupo_fecha
    on comentarios_equipos (id_grupo_equipo, fecha_creacion, estado_eliminado);

create index ix_comentarios_equipos_padre_estado
    on comentarios_equipos (id_comentario_padre, estado_eliminado);

create view vw_equipos_necesitan_mantenimiento
            (codigo_imt, grupo_equipo, estado_equipo, ubicacion, ultima_fecha_mantenimiento) as
SELECT e.codigo_imt,
       ge.nombre                                                    AS grupo_equipo,
       e.estado_equipo,
       e.ubicacion,
       COALESCE(max(m.fecha_mantenimiento), e.fecha_ingreso_equipo) AS ultima_fecha_mantenimiento
FROM equipos e
         LEFT JOIN detalles_mantenimientos dm ON dm.id_equipo = e.id_equipo AND dm.estado_eliminado = false
         JOIN grupos_equipos ge ON ge.id_grupo_equipo = e.id_grupo_equipo
         LEFT JOIN mantenimientos m ON m.id_mantenimiento = dm.id_mantenimiento AND m.estado_eliminado = false
WHERE e.estado_eliminado = false
GROUP BY e.codigo_imt, ge.nombre, e.estado_equipo, e.ubicacion, e.fecha_ingreso_equipo
HAVING (e.estado_equipo = ANY (ARRAY ['parcialmente_operativo'::estado_equipo, 'inoperativo'::estado_equipo]))
    OR max(m.fecha_mantenimiento) IS NOT NULL AND EXTRACT(month FROM age(CURRENT_DATE::timestamp with time zone,
                                                                         max(m.fecha_mantenimiento)::timestamp with time zone)) >
                                                  4::numeric
    OR max(m.fecha_mantenimiento) IS NULL AND EXTRACT(month FROM age(CURRENT_DATE::timestamp with time zone,
                                                                     e.fecha_ingreso_equipo::timestamp with time zone)) >
                                              4::numeric;

alter table vw_equipos_necesitan_mantenimiento
    owner to postgres;

create view vw_ubicaciones_grupos_equipos
            (id_grupo_equipo, codigo_imt, nombre, modelo, marca, ubicacion, categoria, url_imagen) as
SELECT ge.id_grupo_equipo,
       e.codigo_imt,
       ge.nombre,
       ge.modelo,
       ge.marca,
       e.ubicacion,
       c.nombre AS categoria,
       ge.url_imagen
FROM grupos_equipos ge
         JOIN equipos e ON e.id_grupo_equipo = ge.id_grupo_equipo
         JOIN categorias c ON c.id_categoria = ge.id_categoria
         LEFT JOIN gaveteros ga ON e.id_gavetero = ga.id_gavetero
         JOIN muebles mu ON mu.id_mueble = ga.id_mueble
WHERE ge.estado_eliminado = false
  AND e.estado_eliminado = false;

alter table vw_ubicaciones_grupos_equipos
    owner to postgres;

create function digest(text, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function digest(text, text) owner to postgres;

create function digest(bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function digest(bytea, text) owner to postgres;

create function hmac(text, text, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function hmac(text, text, text) owner to postgres;

create function hmac(bytea, bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function hmac(bytea, bytea, text) owner to postgres;

create function crypt(text, text) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function crypt(text, text) owner to postgres;

create function gen_salt(text) returns text
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function gen_salt(text) owner to postgres;

create function gen_salt(text, integer) returns text
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function gen_salt(text, integer) owner to postgres;

create function encrypt(bytea, bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function encrypt(bytea, bytea, text) owner to postgres;

create function decrypt(bytea, bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function decrypt(bytea, bytea, text) owner to postgres;

create function encrypt_iv(bytea, bytea, bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function encrypt_iv(bytea, bytea, bytea, text) owner to postgres;

create function decrypt_iv(bytea, bytea, bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function decrypt_iv(bytea, bytea, bytea, text) owner to postgres;

create function gen_random_bytes(integer) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function gen_random_bytes(integer) owner to postgres;

create function gen_random_uuid() returns uuid
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function gen_random_uuid() owner to postgres;

create function pgp_sym_encrypt(text, text) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_encrypt(text, text) owner to postgres;

create function pgp_sym_encrypt_bytea(bytea, text) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_encrypt_bytea(bytea, text) owner to postgres;

create function pgp_sym_encrypt(text, text, text) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_encrypt(text, text, text) owner to postgres;

create function pgp_sym_encrypt_bytea(bytea, text, text) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_encrypt_bytea(bytea, text, text) owner to postgres;

create function pgp_sym_decrypt(bytea, text) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_decrypt(bytea, text) owner to postgres;

create function pgp_sym_decrypt_bytea(bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_decrypt_bytea(bytea, text) owner to postgres;

create function pgp_sym_decrypt(bytea, text, text) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_decrypt(bytea, text, text) owner to postgres;

create function pgp_sym_decrypt_bytea(bytea, text, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_sym_decrypt_bytea(bytea, text, text) owner to postgres;

create function pgp_pub_encrypt(text, bytea) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_encrypt(text, bytea) owner to postgres;

create function pgp_pub_encrypt_bytea(bytea, bytea) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_encrypt_bytea(bytea, bytea) owner to postgres;

create function pgp_pub_encrypt(text, bytea, text) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_encrypt(text, bytea, text) owner to postgres;

create function pgp_pub_encrypt_bytea(bytea, bytea, text) returns bytea
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_encrypt_bytea(bytea, bytea, text) owner to postgres;

create function pgp_pub_decrypt(bytea, bytea) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_decrypt(bytea, bytea) owner to postgres;

create function pgp_pub_decrypt_bytea(bytea, bytea) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_decrypt_bytea(bytea, bytea) owner to postgres;

create function pgp_pub_decrypt(bytea, bytea, text) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_decrypt(bytea, bytea, text) owner to postgres;

create function pgp_pub_decrypt_bytea(bytea, bytea, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_decrypt_bytea(bytea, bytea, text) owner to postgres;

create function pgp_pub_decrypt(bytea, bytea, text, text) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_decrypt(bytea, bytea, text, text) owner to postgres;

create function pgp_pub_decrypt_bytea(bytea, bytea, text, text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_pub_decrypt_bytea(bytea, bytea, text, text) owner to postgres;

create function pgp_key_id(bytea) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_key_id(bytea) owner to postgres;

create function armor(bytea) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function armor(bytea) owner to postgres;

create function armor(bytea, text[], text[]) returns text
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function armor(bytea, text[], text[]) owner to postgres;

create function dearmor(text) returns bytea
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function dearmor(text) owner to postgres;

create function pgp_armor_headers(text, out key text, out value text) returns setof record
    immutable
    strict
    parallel safe
    language c
as
$$
begin
-- missing source code
end;
$$;

alter function pgp_armor_headers(text, out text, out text) owner to postgres;

create procedure actualizar_accesorio(IN p_id_accesorio_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_codigo_imt_nuevo integer DEFAULT NULL::integer, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_precio_nuevo double precision DEFAULT NULL::double precision, IN p_url_data_sheet_nueva text DEFAULT NULL::text)
    language plpgsql
as
$$
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

alter procedure actualizar_accesorio(integer, varchar, varchar, varchar, integer, text, double precision, text) owner to postgres;

create procedure actualizar_cantidad_grupos_equipos()
    language plpgsql
as
$$
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

alter procedure actualizar_cantidad_grupos_equipos() owner to postgres;

create procedure actualizar_carrera(IN p_id_carrera_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying)
    language plpgsql
as
$$
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

alter procedure actualizar_carrera(integer, varchar) owner to postgres;

create procedure actualizar_categoria(IN p_id_categoria_actualizar integer, IN p_nombre_nuevo_raw character varying DEFAULT NULL::character varying)
    language plpgsql
as
$$
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

alter procedure actualizar_categoria(integer, varchar) owner to postgres;

create procedure actualizar_componente(IN p_id_componente_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_codigo_imt_nuevo integer DEFAULT NULL::integer, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_precio_referencia_nuevo double precision DEFAULT NULL::double precision, IN p_url_data_sheet_nueva text DEFAULT NULL::text)
    language plpgsql
as
$$
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

alter procedure actualizar_componente(integer, varchar, varchar, varchar, integer, text, double precision, text) owner to postgres;

create procedure actualizar_empresa_mantenimiento(IN p_id_empresa_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_nombre_responsable_nuevo character varying DEFAULT NULL::character varying, IN p_apellido_responsable_nuevo character varying DEFAULT NULL::character varying, IN p_telefono_nuevo character varying DEFAULT NULL::character varying, IN p_direccion_nueva text DEFAULT NULL::text, IN p_nit_nuevo character varying DEFAULT NULL::character varying)
    language plpgsql
as
$$
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

alter procedure actualizar_empresa_mantenimiento(integer, varchar, varchar, varchar, varchar, text, varchar) owner to postgres;

create procedure actualizar_equipo(IN p_id_equipo_actualizar integer, IN p_nombre_grupo_equipo_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_grupo_equipo_nuevo character varying DEFAULT NULL::character varying, IN p_marca_grupo_equipo_nuevo character varying DEFAULT NULL::character varying, IN p_codigo_ucb_nuevo character varying DEFAULT NULL::character varying, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_numero_serial_nuevo character varying DEFAULT NULL::character varying, IN p_ubicacion_nueva character varying DEFAULT NULL::character varying, IN p_procedencia_nueva character varying DEFAULT NULL::character varying, IN p_costo_referencia_nuevo double precision DEFAULT NULL::double precision, IN p_tiempo_maximo_prestamo_nuevo integer DEFAULT NULL::integer, IN p_nombre_gavetero_nuevo character varying DEFAULT NULL::character varying, IN p_estado_equipo_nuevo character varying DEFAULT NULL::character varying)
    language plpgsql
as
$$
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

alter procedure actualizar_equipo(integer, varchar, varchar, varchar, varchar, text, varchar, varchar, varchar, double precision, integer, varchar, varchar) owner to postgres;

create procedure actualizar_estado_prestamo(IN p_id_prestamo integer, IN p_estado_prestamo_input estado_prestamo)
    language plpgsql
as
$$
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

alter procedure actualizar_estado_prestamo(integer, estado_prestamo) owner to postgres;

create procedure actualizar_gavetero(IN p_id_gavetero_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_nombre_mueble_nuevo character varying DEFAULT NULL::character varying, IN p_longitud_nueva double precision DEFAULT NULL::double precision, IN p_profundidad_nueva double precision DEFAULT NULL::double precision, IN p_altura_nueva double precision DEFAULT NULL::double precision)
    language plpgsql
as
$$
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

alter procedure actualizar_gavetero(integer, varchar, varchar, varchar, double precision, double precision, double precision) owner to postgres;

create procedure actualizar_grupo_equipo(IN p_id_grupo_equipo_actualizar integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_modelo_nuevo character varying DEFAULT NULL::character varying, IN p_marca_nueva character varying DEFAULT NULL::character varying, IN p_descripcion_nueva text DEFAULT NULL::text, IN p_nombre_categoria_nuevo character varying DEFAULT NULL::character varying, IN p_url_data_sheet_nuevo text DEFAULT NULL::text, IN p_url_imagen_nuevo text DEFAULT NULL::text)
    language plpgsql
as
$$
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

alter procedure actualizar_grupo_equipo(integer, varchar, varchar, varchar, text, varchar, text, text) owner to postgres;

create procedure actualizar_mueble(IN p_id_mueble_actual integer, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_tipo_nuevo character varying DEFAULT NULL::character varying, IN p_costo_nuevo double precision DEFAULT NULL::double precision, IN p_ubicacion_nueva character varying DEFAULT NULL::character varying, IN p_longitud_nueva double precision DEFAULT NULL::double precision, IN p_profundidad_nueva double precision DEFAULT NULL::double precision, IN p_altura_nueva double precision DEFAULT NULL::double precision)
    language plpgsql
as
$$
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

alter procedure actualizar_mueble(integer, varchar, varchar, double precision, varchar, double precision, double precision, double precision) owner to postgres;

create procedure actualizar_usuario(IN p_carnet_actual character varying, IN p_nombre_nuevo character varying DEFAULT NULL::character varying, IN p_apellido_paterno_nuevo character varying DEFAULT NULL::character varying, IN p_apellido_materno_nuevo character varying DEFAULT NULL::character varying, IN p_email_nuevo character varying DEFAULT NULL::character varying, IN p_contrasena_nueva text DEFAULT NULL::text, IN p_rol_nuevo tipo_usuario DEFAULT NULL::tipo_usuario, IN p_carrera_nueva character varying DEFAULT NULL::character varying, IN p_telefono_nuevo character varying DEFAULT NULL::character varying, IN p_telefono_ref_nuevo character varying DEFAULT NULL::character varying, IN p_nombre_ref_nuevo character varying DEFAULT NULL::character varying, IN p_email_ref_nuevo character varying DEFAULT NULL::character varying)
    language plpgsql
as
$$
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

alter procedure actualizar_usuario(varchar, varchar, varchar, varchar, varchar, text, tipo_usuario, varchar, varchar, varchar, varchar, varchar) owner to postgres;

create procedure eliminar_accesorio(IN p_id_accesorio integer)
    language plpgsql
as
$$
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

alter procedure eliminar_accesorio(integer) owner to postgres;

create procedure eliminar_carrera(IN p_id_carrera integer)
    language plpgsql
as
$$
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

alter procedure eliminar_carrera(integer) owner to postgres;

create procedure eliminar_categoria(IN p_id_categoria integer)
    language plpgsql
as
$$
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

alter procedure eliminar_categoria(integer) owner to postgres;

create procedure eliminar_componente(IN p_id_componente integer)
    language plpgsql
as
$$
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

alter procedure eliminar_componente(integer) owner to postgres;

create procedure eliminar_empresas_mantenimiento(IN p_id_empresa_mantenimiento integer)
    language plpgsql
as
$$
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

alter procedure eliminar_empresas_mantenimiento(integer) owner to postgres;

create procedure eliminar_equipo(IN p_id_equipo integer)
    language plpgsql
as
$$
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

alter procedure eliminar_equipo(integer) owner to postgres;

create procedure eliminar_gavetero(IN p_id_gavetero integer)
    language plpgsql
as
$$
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

alter procedure eliminar_gavetero(integer) owner to postgres;

create procedure eliminar_grupo_equipo(IN p_id_grupo_equipo integer)
    language plpgsql
as
$$
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

alter procedure eliminar_grupo_equipo(integer) owner to postgres;

create procedure eliminar_mantenimiento(IN p_id_mantenimiento integer)
    language plpgsql
as
$$
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

alter procedure eliminar_mantenimiento(integer) owner to postgres;

create procedure eliminar_mueble(IN p_id_mueble integer)
    language plpgsql
as
$$
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

alter procedure eliminar_mueble(integer) owner to postgres;

create procedure eliminar_prestamo(IN p_id_prestamo integer)
    language plpgsql
as
$$
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

alter procedure eliminar_prestamo(integer) owner to postgres;

create procedure eliminar_usuario(IN p_carnet character varying)
    language plpgsql
as
$$
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

alter procedure eliminar_usuario(varchar) owner to postgres;

create function fn_actualizar_cantidad_equipo_por_estado() returns trigger
    language plpgsql
as
$$
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

alter function fn_actualizar_cantidad_equipo_por_estado() owner to postgres;

create function fn_actualizar_cantidad_tras_update_equipos() returns trigger
    language plpgsql
as
$$
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

alter function fn_actualizar_cantidad_tras_update_equipos() owner to postgres;

create function fn_actualizar_conteo_gaveteros_por_estado() returns trigger
    language plpgsql
as
$$
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

alter function fn_actualizar_conteo_gaveteros_por_estado() owner to postgres;

create function fn_actualizar_costo_promedio_grupo() returns trigger
    language plpgsql
as
$$
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

alter function fn_actualizar_costo_promedio_grupo() owner to postgres;

create function fn_actualizar_gavetero_tras_update_mueble() returns trigger
    language plpgsql
as
$$
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

alter function fn_actualizar_gavetero_tras_update_mueble() owner to postgres;

create function fn_estado_eliminado_mantenimiento_a_detalle() returns trigger
    language plpgsql
as
$$
BEGIN

    IF OLD.estado_eliminado IS DISTINCT FROM NEW.estado_eliminado THEN
        UPDATE public.detalles_mantenimientos
           SET estado_eliminado = NEW.estado_eliminado 
         WHERE id_mantenimiento = NEW.id_mantenimiento;
    END IF;

    RETURN NEW;
END;
$$;

alter function fn_estado_eliminado_mantenimiento_a_detalle() owner to postgres;

create function fn_estado_eliminado_prestamo_a_detalle() returns trigger
    language plpgsql
as
$$
BEGIN

    IF OLD.estado_eliminado IS DISTINCT FROM NEW.estado_eliminado THEN
        UPDATE public.detalles_prestamos
           SET estado_eliminado = NEW.estado_eliminado 
         WHERE id_prestamo = NEW.id_prestamo;
    END IF;


    RETURN NEW;
END;
$$;

alter function fn_estado_eliminado_prestamo_a_detalle() owner to postgres;

create function fn_incrementar_cantidad_equipos() returns trigger
    language plpgsql
as
$$
BEGIN
  -- Aumenta en 1 la cantidad en grupos_equipos
  UPDATE grupos_equipos
     SET cantidad = cantidad + 1
   WHERE id_grupo_equipo = NEW.id_grupo_equipo;
  RETURN NEW;
END;
$$;

alter function fn_incrementar_cantidad_equipos() owner to postgres;

create function fn_incrementar_numero_gaveteros() returns trigger
    language plpgsql
as
$$
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

alter function fn_incrementar_numero_gaveteros() owner to postgres;

create procedure insertar_accesorios(IN p_nombre character varying, IN p_modelo character varying, IN p_tipo character varying, IN p_codigo_imt integer, IN p_descripcion text DEFAULT NULL::text, IN p_precio double precision DEFAULT NULL::double precision, IN p_url_data_sheet text DEFAULT NULL::text)
    language plpgsql
as
$$
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

alter procedure insertar_accesorios(varchar, varchar, varchar, integer, text, double precision, text) owner to postgres;

create procedure insertar_carrera(IN p_nombre character varying)
    language plpgsql
as
$$
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

alter procedure insertar_carrera(varchar) owner to postgres;

create procedure insertar_categoria(IN p_nombre_raw character varying)
    language plpgsql
as
$$
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

alter procedure insertar_categoria(varchar) owner to postgres;

create procedure insertar_componente(IN p_nombre character varying, IN p_modelo character varying, IN p_tipo character varying, IN p_codigo_imt integer, IN p_descripcion text DEFAULT NULL::text, IN p_precio_referencia double precision DEFAULT NULL::double precision, IN p_url_data_sheet text DEFAULT NULL::text)
    language plpgsql
as
$$
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

alter procedure insertar_componente(varchar, varchar, varchar, integer, text, double precision, text) owner to postgres;

create procedure insertar_empresa_mantenimiento(IN p_nombre character varying, IN p_nombre_responsable character varying, IN p_apellido_responsable character varying, IN p_telefono character varying, IN p_direccion text, IN p_nit character varying)
    language plpgsql
as
$$
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

alter procedure insertar_empresa_mantenimiento(varchar, varchar, varchar, varchar, text, varchar) owner to postgres;

create procedure insertar_equipo(IN p_nombre_grupo_equipo character varying, IN p_modelo character varying, IN p_marca character varying, IN p_codigo_ucb character varying, IN p_descripcion text, IN p_numero_serial character varying, IN p_ubicacion character varying, IN p_procedencia character varying, IN p_costo_referencia double precision, IN p_tiempo_maximo_prestamo integer, IN p_nombre_gavetero character varying)
    language plpgsql
as
$$
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

alter procedure insertar_equipo(varchar, varchar, varchar, varchar, text, varchar, varchar, varchar, double precision, integer, varchar) owner to postgres;

create procedure insertar_gavetero(IN p_nombre character varying, IN p_tipo character varying, IN p_nombre_mueble character varying, IN p_longitud double precision, IN p_profundidad double precision, IN p_altura double precision)
    language plpgsql
as
$$
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

alter procedure insertar_gavetero(varchar, varchar, varchar, double precision, double precision, double precision) owner to postgres;

create procedure insertar_grupo_equipo(IN p_nombre character varying, IN p_modelo character varying, IN p_marca character varying, IN p_descripcion text, IN p_nombre_categoria character varying, IN p_url_data_sheet text, IN p_url_imagen text)
    language plpgsql
as
$$
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

alter procedure insertar_grupo_equipo(varchar, varchar, varchar, text, varchar, text, text) owner to postgres;

create procedure insertar_mantenimiento(IN p_fecha_mantenimiento date, IN p_fecha_final_mantenimiento date, IN p_nombre_empresa character varying, IN p_costo double precision, IN p_descripcion text, IN p_codigos_imt integer[], IN p_tipos_mantenimiento character varying[], IN p_descripciones_equipo text[])
    language plpgsql
as
$$
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

alter procedure insertar_mantenimiento(date, date, varchar, double precision, text, integer[], character varying[], text[]) owner to postgres;

create procedure insertar_mueble(IN p_nombre character varying, IN p_tipo character varying, IN p_costo double precision, IN p_ubicacion character varying, IN p_longitud double precision, IN p_profundidad double precision, IN p_altura double precision)
    language plpgsql
as
$$
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

alter procedure insertar_mueble(varchar, varchar, double precision, varchar, double precision, double precision, double precision) owner to postgres;

create procedure insertar_prestamo(IN id_grupos_equipo_input integer[], IN fecha_prestamo_esperada_input timestamp without time zone, IN fecha_devolucion_esperada_input timestamp without time zone, IN observacion_input text, IN carnet_input character varying, IN id_contrato_input text)
    language plpgsql
as
$$
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

alter procedure insertar_prestamo(integer[], timestamp, timestamp, text, varchar, text) owner to postgres;

create procedure insertar_prestamo(IN id_grupos_equipo_input integer[], IN fecha_prestamo_esperada_input timestamp with time zone, IN fecha_devolucion_esperada_input timestamp with time zone, IN observacion_input text, IN carnet_input character varying, IN id_contrato_input text)
    language plpgsql
as
$$
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

alter procedure insertar_prestamo(integer[], timestamp with time zone, timestamp with time zone, text, varchar, text) owner to postgres;

create procedure insertar_usuario(IN carnet_input character varying, IN nombre_input character varying, IN apellido_paterno_input character varying, IN apellido_materno_input character varying, IN rol_input tipo_usuario, IN email_input character varying, IN contrasena_input text, IN carrera_input character varying, IN telefono_input character varying, IN telefono_referencia_input character varying, IN nombre_referencia_input character varying, IN email_referencia_input character varying)
    language plpgsql
as
$$
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

alter procedure insertar_usuario(varchar, varchar, varchar, varchar, tipo_usuario, varchar, text, varchar, varchar, varchar, varchar, varchar) owner to postgres;

create function insertar_y_obtener_prestamo(id_grupos_equipo_input integer[], fecha_prestamo_esperada_input timestamp without time zone, fecha_devolucion_esperada_input timestamp without time zone, observacion_input text, carnet_input character varying, id_contrato_input text)
    returns TABLE(id_prestamo integer, id_equipo integer, codigo_imt character varying, codigo_serial character varying, nombre character varying, modelo character varying, marca character varying, id_grupo_equipo integer)
    language plpgsql
as
$$
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

alter function insertar_y_obtener_prestamo(integer[], timestamp, timestamp, text, varchar, text) owner to postgres;

create function obtener_accesorios()
    returns TABLE(id_accesorio integer, nombre_accesorio character varying, modelo_accesorio character varying, tipo_accesorio character varying, precio_accesorio double precision, nombre_equipo_asociado character varying, codigo_imt_equipo_asociado integer, descripcion_accesorio text, url_data_sheet_accesorio text)
    language plpgsql
as
$$
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

alter function obtener_accesorios() owner to postgres;

create function obtener_carreras()
    returns TABLE(id_carrera integer, nombre_carrera character varying)
    language plpgsql
as
$$
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

alter function obtener_carreras() owner to postgres;

create function obtener_categorias()
    returns TABLE(id_categoria integer, categoria character varying)
    language plpgsql
as
$$
BEGIN
    RETURN QUERY
	SELECT 
	c.id_categoria,
	c.nombre
	from categorias as c
	where c.estado_eliminado=false;
END;
$$;

alter function obtener_categorias() owner to postgres;

create function obtener_codigo_imt(p_id_grupo_equipo integer) returns integer
    language plpgsql
as
$$
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

alter function obtener_codigo_imt(integer) owner to postgres;

create function obtener_componentes()
    returns TABLE(id_componente integer, nombre_componente character varying, modelo_componente character varying, tipo_componente character varying, descripcion_componente text, precio_referencia_componente double precision, nombre_equipo character varying, codigo_imt_equipo integer, url_data_sheet_equipo text)
    language plpgsql
as
$$
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

alter function obtener_componentes() owner to postgres;

create function obtener_disponibilidad_equipos_por_fechas_y_id_grupos_equipos(fecha_inicio timestamp without time zone, fecha_fin timestamp without time zone, p_array_ids integer[])
    returns TABLE(fecha timestamp without time zone, id_grupo_equipo integer, cantidad_disponible bigint)
    language plpgsql
as
$$
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

alter function obtener_disponibilidad_equipos_por_fechas_y_id_grupos_equipos(timestamp, timestamp, integer[]) owner to postgres;

create function obtener_empresas_mantenimiento()
    returns TABLE(id_empresa_mantenimiento integer, nombre_empresa character varying, nombre_responsable_empresa character varying, apellido_responsable_empresa character varying, telefono_empresa character varying, nit_empresa character varying, direccion_empresa character varying)
    rows 100
    language plpgsql
as
$$
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

alter function obtener_empresas_mantenimiento() owner to postgres;

create function obtener_equipos()
    returns TABLE(id_equipo integer, nombre_grupo_equipo character varying, modelo_equipo character varying, marca_equipo character varying, codigo_imt_equipo integer, codigo_ucb_equipo character varying, numero_serial_equipo character varying, estado_equipo_equipo estado_equipo, ubicacion_equipo character varying, nombre_gavetero_equipo character varying, costo_referencia_equipo double precision, descripcion_equipo text, tiempo_max_prestamo_equipo integer, procedencia_equipo character varying)
    language plpgsql
as
$$
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

alter function obtener_equipos() owner to postgres;

create function obtener_equipos_necesitan_mantenimiento()
    returns TABLE(id_equipo integer, codigo_imt integer, nombre character varying, estado_equipo estado_equipo, ubicacion character varying, ultima_fecha_mantenimiento date)
    language plpgsql
as
$$
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

alter function obtener_equipos_necesitan_mantenimiento() owner to postgres;

create function obtener_fechas_no_disponibles_por_id_grupos_equipos(fecha_inicio timestamp without time zone, fecha_fin timestamp without time zone, json_input jsonb)
    returns TABLE(id_grupo_equipo integer, fecha_no_disponible timestamp without time zone, cantidad_disponible bigint)
    language plpgsql
as
$$
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

alter function obtener_fechas_no_disponibles_por_id_grupos_equipos(timestamp, timestamp, jsonb) owner to postgres;

create function obtener_gaveteros()
    returns TABLE(id_gavetero integer, nombre_gavetero character varying, tipo_gavetero character varying, nombre_mueble character varying, longitud_gavetero double precision, profundidad_gavetero double precision, altura_gavetero double precision)
    rows 100
    language plpgsql
as
$$
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

alter function obtener_gaveteros() owner to postgres;

create function obtener_grupo_equipo_especifico_por_id(id_grupo_equipo_input integer)
    returns TABLE(id_grupo_equipo integer, nombre_grupo_equipo character varying, modelo_grupo_equipo character varying, marca_grupo_equipo character varying, descripcion_grupo_equipo text, url_data_sheet_grupo_equipo text, nombre_categoria character varying, url_imagen_grupo_equipo text, cantidad_grupo_equipo integer, costo_promedio numeric)
    language plpgsql
as
$$
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

alter function obtener_grupo_equipo_especifico_por_id(integer) owner to postgres;

create function obtener_grupos_equipos()
    returns TABLE(id_grupo_equipo integer, nombre_grupo_equipo character varying, modelo_grupo_equipo character varying, marca_grupo_equipo character varying, nombre_categoria character varying, cantidad_grupo_equipo integer, descripcion_grupo_equipo text, url_data_sheet_grupo_equipo text, url_imagen_grupo_equipo text, costo_promedio numeric)
    rows 100
    language plpgsql
as
$$
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

alter function obtener_grupos_equipos() owner to postgres;

create function obtener_grupos_equipos_por_nombre_y_categoria(nombre_grupo_equipo_input text, categoria_input text)
    returns TABLE(id_grupo_equipo integer, nombre_grupo_equipo character varying, modelo_grupo_equipo character varying, marca_grupo_equipo character varying, nombre_categoria character varying, url_imagen_grupo_equipo text, url_data_sheet_grupo_equipo text, descripcion_grupo_equipo text, cantidad_grupo_equipo integer, costo_promedio numeric)
    language plpgsql
as
$$
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

alter function obtener_grupos_equipos_por_nombre_y_categoria(text, text) owner to postgres;

create function obtener_mantenimientos()
    returns TABLE(id_mantenimiento integer, nombre_empresa_mantenimiento character varying, fecha_mantenimiento date, fecha_final_mantenimiento date, costo_mantenimiento double precision, descripcion_mantenimiento text, tipo_detalle_mantenimiento character varying, nombre_grupo_equipo character varying, codigo_imt_equipo integer, descripcion_equipo text)
    language plpgsql
as
$$
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

alter function obtener_mantenimientos() owner to postgres;

create function obtener_muebles()
    returns TABLE(id_mueble integer, nombre_mueble character varying, numero_gaveteros_mueble integer, ubicacion_mueble character varying, tipo_mueble character varying, costo_mueble double precision, longitud_mueble double precision, profundidad_mueble double precision, altura_mueble double precision)
    rows 100
    language plpgsql
as
$$
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

alter function obtener_muebles() owner to postgres;

create function obtener_numero_equipos_disponibles_por_id_y_fechas(id_grupo_equipo_input integer, fecha_prestamo_esperada_input timestamp without time zone, fecha_devolucion_esperada_input timestamp without time zone)
    returns TABLE(cantidad_disponible bigint)
    language plpgsql
as
$$
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

alter function obtener_numero_equipos_disponibles_por_id_y_fechas(integer, timestamp, timestamp) owner to postgres;

create function obtener_prestamos()
    returns TABLE(id_prestamo integer, carnet character varying, nombre character varying, apellido_paterno character varying, telefono character varying, nombre_grupo_equipo character varying, codigo_imt integer, fecha_solicitud timestamp without time zone, fecha_prestamo_esperada timestamp without time zone, fecha_prestamo timestamp without time zone, fecha_devolucion_esperada timestamp without time zone, fecha_devolucion timestamp without time zone, observacion text, estado_prestamo estado_prestamo, ubicacion_equipo character varying, nombre_gavetero character varying, nombre_mueble character varying, ubicacion_mueble character varying)
    language plpgsql
as
$$
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

alter function obtener_prestamos() owner to postgres;

create function obtener_prestamos_por_carnet_y_estado_prestamo(p_carnet_input character varying, p_estado_input estado_prestamo)
    returns TABLE(id_prestamo integer, carnet character varying, nombre character varying, apellido_paterno character varying, telefono character varying, nombre_grupo_equipo character varying, codigo_imt integer, fecha_solicitud timestamp without time zone, fecha_prestamo_esperada timestamp without time zone, fecha_prestamo timestamp without time zone, fecha_devolucion_esperada timestamp without time zone, fecha_devolucion timestamp without time zone, observacion text, estado_prestamo estado_prestamo, ubicacion_equipo character varying, nombre_gavetero character varying, nombre_mueble character varying, ubicacion_mueble character varying)
    language plpgsql
as
$$
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

alter function obtener_prestamos_por_carnet_y_estado_prestamo(varchar, estado_prestamo) owner to postgres;

create function obtener_ubicaciones_grupos_equipos_por_nombre(nombre_grupo_equipo_input text)
    returns TABLE(id_grupo_equipo integer, codigo_imt integer, nombre character varying, modelo character varying, marca character varying, ubicacion character varying, categoria character varying, url_imagen text)
    language plpgsql
as
$$
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

alter function obtener_ubicaciones_grupos_equipos_por_nombre(text) owner to postgres;

create function obtener_usuario_iniciar_sesion(email_input character varying, contrasena_input text)
    returns TABLE(carnet character varying, nombre character varying, apellido_paterno character varying, apellido_materno character varying, rol tipo_usuario, carrera character varying, email character varying, telefono character varying, telefono_referencia character varying, nombre_referencia character varying, email_referencia character varying)
    language plpgsql
as
$$
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

alter function obtener_usuario_iniciar_sesion(varchar, text) owner to postgres;

create function obtener_usuario_por_carnet(carnet_input text)
    returns TABLE(carnet character varying, nombre character varying, apellido_paterno character varying, apellido_materno character varying, rol tipo_usuario, carrera character varying, email character varying, contrasena text, telefono character varying, telefono_referencia character varying, nombre_referencia character varying, email_referencia character varying)
    language plpgsql
as
$$
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

alter function obtener_usuario_por_carnet(text) owner to postgres;

create function obtener_usuarios()
    returns TABLE(carnet character varying, nombre character varying, apellido_paterno character varying, apellido_materno character varying, carrera character varying, rol tipo_usuario, email character varying, telefono character varying, telefono_referencia character varying, nombre_referencia character varying, email_referencia character varying)
    language plpgsql
as
$$
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

alter function obtener_usuarios() owner to postgres;

create procedure recalcular_costo_promedio_todos_grupos()
    language plpgsql
as
$$
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

alter procedure recalcular_costo_promedio_todos_grupos() owner to postgres;


CREATE TABLE IF NOT EXISTS configuraciones_sistema (
    horarios jsonb NOT NULL DEFAULT '[]'::jsonb,
    id_configuracion integer GENERATED BY DEFAULT AS IDENTITY,
    monto_minimo_contrato numeric NOT NULL,
    horario_inicio_minutos integer NOT NULL,
    horario_fin_minutos integer NOT NULL,
    nombre_jefe_carrera text NOT NULL,
    firma_jefe_carrera_base64 text NOT NULL,
    tiempo_minimo_reserva_minutos integer NOT NULL,
    tiempo_recordatorio_previo_minutos integer NOT NULL,
    minutos_gracia_atraso integer NOT NULL,
    estado_eliminado boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_configuraciones_sistema PRIMARY KEY (id_configuracion)
);
