using FluentValidation;

namespace Felix.Api.Endpoints.Weather.GetTaiwanWeather;

/// <summary>
/// 取得台灣天氣請求驗證器
/// </summary>
public sealed class GetTaiwanWeatherValidator : AbstractValidator<GetTaiwanWeatherRequest>
{
    public GetTaiwanWeatherValidator()
    {
        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("請提供地點名稱");
    }
}
