-- Coach Desk: ciclos de asesoría para mediciones y fotografías.
-- Cambio aditivo y reversible a nivel de datos: los registros actuales quedan
-- asignados al ciclo 1. Ejecutar una sola vez desde Supabase SQL Editor.

begin;

alter table public.mediciones add column if not exists periodo smallint;
update public.mediciones set periodo = 1 where periodo is null;
alter table public.mediciones alter column periodo set default 1;
alter table public.mediciones alter column periodo set not null;
alter table public.mediciones drop constraint if exists ck_mediciones_periodo;
alter table public.mediciones add constraint ck_mediciones_periodo check (periodo >= 1 and periodo <= 1000);
alter table public.mediciones drop constraint if exists uq_mediciones_cliente_mes;
alter table public.mediciones drop constraint if exists uq_mediciones_cliente_periodo_mes;
alter table public.mediciones add constraint uq_mediciones_cliente_periodo_mes unique (numero_cliente, periodo, mes);

alter table public.fotos_control add column if not exists periodo smallint;
update public.fotos_control set periodo = 1 where periodo is null;
alter table public.fotos_control alter column periodo set default 1;
alter table public.fotos_control alter column periodo set not null;
alter table public.fotos_control drop constraint if exists ck_fotos_control_periodo;
alter table public.fotos_control add constraint ck_fotos_control_periodo check (periodo >= 1 and periodo <= 1000);
alter table public.fotos_control drop constraint if exists fotos_control_numero_cliente_mes_tipo_key;
alter table public.fotos_control drop constraint if exists uq_fotos_control_cliente_periodo_mes_tipo;
alter table public.fotos_control add constraint uq_fotos_control_cliente_periodo_mes_tipo unique (numero_cliente, periodo, mes, tipo);

commit;
