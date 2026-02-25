using FluentValidation;

namespace Felix.Api.Endpoints.Weather.SearchWeather;

public class SearchWeatherValidator : AbstractValidator<SearchWeatherRequest>
{
    public SearchWeatherValidator()
    {
        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required");
    }
}
