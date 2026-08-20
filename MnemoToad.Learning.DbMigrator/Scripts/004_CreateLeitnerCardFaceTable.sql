CREATE TABLE leitner_card_face (
    leitner_card_id UUID NOT NULL,
    property_path VARCHAR(100) NOT NULL,
    face_index INT NOT NULL,
    content JSONB NOT NULL,
    CONSTRAINT pk_leitner_card_face PRIMARY KEY (leitner_card_id, property_path),
    CONSTRAINT fk_leitner_card_face_leitner_card_id FOREIGN KEY (leitner_card_id) REFERENCES leitner_card(id) ON DELETE CASCADE,
    CONSTRAINT uq_leitner_card_face_leitner_card_id_face_index UNIQUE (leitner_card_id, face_index) DEFERRABLE INITIALLY DEFERRED
);
