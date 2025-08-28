-- Create Schedules table
CREATE TABLE public."Schedule" (
                                   "Id" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                                   "Name" text NOT NULL,
                                   "Type" text NOT NULL,
                                   "SubType" text NULL,
                                   "StartDateTime" timestamp without time zone NOT NULL,
                                   "EndDateTime" timestamp without time zone  NULL,
                                   "Details" text NULL,
                                   "NoOfDays" int NULL,
                                   "StartDays" text NULL,
                                   "StartCronExp" text NULL,
                                   "StopCronExp" text NULL,
                                   "Status" text NULL,
                                   "RecurringTime" timestamp without time zone NULL,
                                   CONSTRAINT "PK_Schedule" PRIMARY KEY ("Id")
);

-- Create ScheduleResourceMapping table

CREATE TABLE public."ScheduleResourceMapping" (
    "Id" UUID PRIMARY KEY,
    "ScheduleId" UUID NOT NULL,
    "ResourceId" UUID NOT NULL,
    "ResourceType" VARCHAR(50) NOT NULL,
    "metaData" text NULL,
    FOREIGN KEY ("ScheduleId") REFERENCES public."Schedule"("Id") ON DELETE CASCADE
);


ALTER TABLE public."ScheduleResourceMapping"
    ADD CONSTRAINT uk_schedule_resource_type
        UNIQUE ("ScheduleId", "ResourceId", "ResourceType");


INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ('b8270000-2700-0a00-cea0-08dc006fdea8', 'Rights', 'ShowScheduleTab');
INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ('b8270000-2700-0a00-cea0-08dc006fdea8', 'Rights', 'AddSchedule');
INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ('b8270000-2700-0a00-cea0-08dc006fdea8', 'Rights', 'DeleteSchedule');
