-- Create Schedules table
CREATE TABLE IF NOT EXISTS public."Schedule" (
                                                 "Id" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                                                 "Name" text NOT NULL,
                                                 "Type" text NOT NULL,
                                                 "SubType" text NULL,
                                                 "StartDateTime" timestamp without time zone NOT NULL,
                                                 "EndDateTime" timestamp without time zone NOT NULL,
                                                 "Details" text NULL,
                                                 "NoOfDays" int NULL,
                                                 "StartDays" text NULL,
                                                 "StartCronExp" text NULL,
                                                 "StopCronExp" text NULL,
                                                 CONSTRAINT "PK_Schedules" PRIMARY KEY ("Id")
    );

-- Create ScheduleResourceMapping table
CREATE TABLE schedule_resources (
                                    id UUID PRIMARY KEY,
                                    schedule_id UUID NOT NULL,
                                    resource_id UUID NOT NULL,
                                    resource_type VARCHAR(50) NOT NULL,
                                    FOREIGN KEY (schedule_id) REFERENCES schedules(id) ON DELETE CASCADE,
                                    INDEX idx_schedule_resources (schedule_id),
                                    INDEX idx_resource_lookup (resource_id, resource_type)
);

ALTER TABLE schedule_resources
    ADD CONSTRAINT uk_schedule_resource_type
        UNIQUE (schedule_id, resource_id, resource_type);

