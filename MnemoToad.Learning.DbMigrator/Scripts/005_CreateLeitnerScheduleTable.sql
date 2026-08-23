CREATE TABLE leitner_schedule (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    box_number INT NOT NULL,
    interval_hours INT NOT NULL,
    variance_hours INT NOT NULL
);
