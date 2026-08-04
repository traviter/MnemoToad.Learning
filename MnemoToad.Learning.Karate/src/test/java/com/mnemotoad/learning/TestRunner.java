package com.mnemotoad.learning;

import com.intuit.karate.Runner;
import com.intuit.karate.Results;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;

class TestRunner {

    @Test
    void testParallel() {
        String tagsProperty = System.getProperty("karate.tags");

        Runner.Builder<?> builder = Runner.path("classpath:com/mnemotoad/learning")
                .outputCucumberJson(true);

        if (tagsProperty != null && !tagsProperty.isBlank()) {
            // A single expression string, not one-element-per-tag: Karate itself treats commas
            // within an expression as OR, and separate list elements as AND.
            builder = builder.tags(List.of(tagsProperty));
        }

        Results results = builder.parallel(5);
        assertEquals(0, results.getFailCount(), results.getErrorMessages());
    }
}
