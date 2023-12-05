package credits.currency.api;

import io.opentelemetry.api.baggage.Baggage;
import io.opentelemetry.api.trace.Span;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class CurrencyController {
    private static final Logger logger = LoggerFactory.getLogger(CurrencyController.class);

    @GetMapping("/v1/currency-rate/from/{fromCurrency}/to/{toCurrency}")
    public CurrencyConversionRateResponse convertCurrencyRate(@PathVariable String fromCurrency, @PathVariable("toCurrency") String toCurrency) {
        Span span = Span.current();
        Baggage.current().asMap().forEach((s, baggageEntry) -> span.setAttribute(s, baggageEntry.getValue()));
        logger.info("getting currency rate from {} to {}", fromCurrency, toCurrency);

        if (fromCurrency.equals("USD")) {
            String rate = switch (toCurrency) {
                case "SEK" -> "10.98";
                case "EUR" -> "0.95";
                case "GBP" -> "0.82";
                default -> "1.0";
            };
            return new CurrencyConversionRateResponse(rate);
        }

        if (fromCurrency.equals("EUR")) {
            String rate = switch (toCurrency) {
                case "USD" -> "1.05";
                case "SEK" -> "10.33";
                case "GBP" -> "0.86";
                default -> "1.0";
            };
            return new CurrencyConversionRateResponse(rate);
        }

        if (fromCurrency.equals("SEK")) {
            String rate = switch (toCurrency) {
                case "USD" -> "0.091";
                case "EUR" -> "0.086";
                case "GBP" -> "0.068";
                default -> "1.0";
            };
            return new CurrencyConversionRateResponse(rate);
        }


        return new CurrencyConversionRateResponse("1");
    }
}

