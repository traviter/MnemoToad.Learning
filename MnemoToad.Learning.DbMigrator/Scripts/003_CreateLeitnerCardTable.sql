CREATE TABLE leitner_card (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    deck_id UUID NOT NULL,
    node_id UUID,
    box_number INT NOT NULL DEFAULT 0,
    due_utc TIMESTAMP,
    last_reviewed_utc TIMESTAMP,
    CONSTRAINT fk_leitner_card_deck_id FOREIGN KEY (deck_id) REFERENCES leitner_deck(id) ON DELETE CASCADE
);
