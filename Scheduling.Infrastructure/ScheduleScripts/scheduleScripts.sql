-- Create Schedules table
CREATE TABLE  IF NOT EXISTS public."Schedule" (
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
          AND conrelid = 'public."Schedule"'::regclass
    ) THEN
ALTER TABLE public."Schedule"
    ADD CONSTRAINT "UK_Schedule_Name" UNIQUE ("Name");
END IF;
END;
$$;

-- Create ScheduleResourceMapping table

CREATE TABLE  IF NOT EXISTS  public."ScheduleResourceMapping" (
    "Id" UUID PRIMARY KEY,
    "ScheduleId" UUID NOT NULL,
    "ResourceId" UUID NOT NULL,
    "ResourceType" VARCHAR(50) NOT NULL,
    "metaData" text NULL,
    FOREIGN KEY ("ScheduleId") REFERENCES public."Schedule"("Id") ON DELETE CASCADE
);
ALTER TABLE public."ScheduleResourceMapping"
DROP CONSTRAINT if exists uk_schedule_resource_type;

DO $$

     
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uk_schedule_resource'
    ) THEN
ALTER TABLE public."ScheduleResourceMapping"
    ADD CONSTRAINT uk_schedule_resource
        UNIQUE ("Id");
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

INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ('b8270000-2700-0a00-cea0-08dc006fdea8', 'Rights', 'ShowScheduleTab')
    ON CONFLICT ("RoleId", "ClaimType", "ClaimValue") DO NOTHING;
INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ('b8270000-2700-0a00-cea0-08dc006fdea8', 'Rights', 'AddSchedule')
    ON CONFLICT ("RoleId", "ClaimType", "ClaimValue") DO NOTHING;
INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ('b8270000-2700-0a00-cea0-08dc006fdea8', 'Rights', 'DeleteSchedule')
    ON CONFLICT ("RoleId", "ClaimType", "ClaimValue") DO NOTHING;
