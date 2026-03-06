-- Drop Schedule table if Type column is text
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'scheduler'
          AND table_name = 'Schedule'
          AND column_name = 'Type'
          AND data_type = 'text'
    ) THEN
        DROP TABLE IF EXISTS scheduler."Schedule" CASCADE;
        RAISE NOTICE 'Schedule table dropped because Type column was text type';
    END IF;
END$$;


-- Create Schedules table
CREATE TABLE  IF NOT EXISTS scheduler."Schedule" (
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
                                    CONSTRAINT "PK_Schedule" PRIMARY KEY ("Id")

);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'UK_Schedule_Name'
          AND conrelid = 'scheduler."Schedule"'::regclass
    ) THEN
ALTER TABLE scheduler."Schedule"
    ADD CONSTRAINT "UK_Schedule_Name" UNIQUE ("Name");
END IF;
END;
$$;

-- Create ScheduleResourceMapping table

CREATE TABLE  IF NOT EXISTS  scheduler."ScheduleResourceMapping" (
    "Id" UUID PRIMARY KEY,
    "ScheduleId" UUID NOT NULL,
    "ResourceId" UUID NOT NULL,
    "ResourceType" VARCHAR(50) NOT NULL,
    "metaData" text NULL,
    FOREIGN KEY ("ScheduleId") REFERENCES scheduler."Schedule"("Id") ON DELETE CASCADE
);
ALTER TABLE scheduler."ScheduleResourceMapping"
DROP CONSTRAINT if exists uk_schedule_resource_type;


DO $$
BEGIN
    IF  EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uk_schedule_resource'
    ) THEN
ALTER TABLE scheduler."ScheduleResourceMapping"
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
ALTER TABLE scheduler."ScheduleResourceMapping"
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

DO $$
DECLARE
admin_role_id UUID;
BEGIN
    -- Get the Administrator role ID
SELECT "Id" INTO admin_role_id
FROM public."AspNetRoles"
WHERE "NormalizedName" = 'ADMINISTRATOR';

IF admin_role_id IS NOT NULL THEN
        -- Insert claims for Administrator role
        INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue")
        VALUES
            (admin_role_id, 'Rights', 'ShowScheduleTab'),
            (admin_role_id, 'Rights', 'AddSchedule'),
            (admin_role_id, 'Rights', 'DeleteSchedule')
        ON CONFLICT ("RoleId", "ClaimType", "ClaimValue") DO NOTHING;
END IF;
END$$;


