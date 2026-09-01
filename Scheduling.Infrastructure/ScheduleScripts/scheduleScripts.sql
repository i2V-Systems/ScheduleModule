-- Drop Schedules table if Type column is text
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Schedules'
          AND column_name = 'Type'
          AND data_type = 'text'
    ) THEN
        DROP TABLE IF EXISTS public."Schedules" CASCADE;
        RAISE NOTICE 'Schedules table dropped because Type column was text type';
    END IF;
END$$;


-- Create Schedules table
CREATE TABLE  IF NOT EXISTS public."Schedules" (
                                    "Id" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                                    "Name" text NOT NULL,
                                    "Type" integer NOT NULL,
                                    "SubType" integer NULL,
                                    "StartDateTime" timestamp without time zone NOT NULL,
                                    "EndDateTime" timestamp without time zone  NULL,
                                    "Details" text NULL,
                                    "NoOfDays" integer NULL,
                                    "StartDays" text NULL,
                                    "StartCronExp" text NULL,
                                    "StopCronExp" text NULL,
                                    "Status" integer NULL,
                                    "RecurringTime" timestamp without time zone NULL,
                                    CONSTRAINT "PK_Schedules" PRIMARY KEY ("Id")

);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'UK_Schedules_Name'
          AND conrelid = 'public."Schedules"'::regclass
    ) THEN
ALTER TABLE public."Schedules"
    ADD CONSTRAINT "UK_Schedules_Name" UNIQUE ("Name");
END IF;
END;
$$;

-- Create ScheduleResourceMapping table

CREATE TABLE  IF NOT EXISTS  public."ScheduleResourceMapping" (
    "Id" UUID PRIMARY KEY,
    "ScheduleId" UUID NOT NULL,
    "ResourceId" VARCHAR(100) NOT NULL,
    "ResourceType" VARCHAR(50) NOT NULL,
    "metaData" text NULL,
    FOREIGN KEY ("ScheduleId") REFERENCES public."Schedules"("Id") ON DELETE CASCADE
);
ALTER TABLE public."ScheduleResourceMapping"
DROP CONSTRAINT if exists uk_schedule_resource_type;


DO $$
BEGIN
    IF  EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uk_schedule_resource'
    ) THEN
ALTER TABLE public."ScheduleResourceMapping"
DROP CONSTRAINT uk_schedule_resource;
END IF;
END$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uk_schedule_resource'
    ) THEN
ALTER TABLE public."ScheduleResourceMapping"
    ADD CONSTRAINT uk_schedule_resource
      UNIQUE ("ScheduleId", "ResourceId", "ResourceType");
END IF;
END$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_role_claim'
    ) THEN
ALTER TABLE public."AspNetRoleClaims"
  ADD CONSTRAINT uq_role_claim UNIQUE ("RoleId", "ClaimType", "ClaimValue");
END IF;
END$$;


ALTER TABLE public."Schedules"
ALTER COLUMN "StartDateTime" TYPE timestamp with time zone
  USING "StartDateTime" AT TIME ZONE 'UTC';

ALTER TABLE public."Schedules"
ALTER COLUMN "EndDateTime" TYPE timestamp with time zone
  USING "EndDateTime" AT TIME ZONE 'UTC';
