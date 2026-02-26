using FluentValidation;

namespace Felix.Api.Endpoints.Assistant.Process;

public class ProcessValidator : AbstractValidator<ProcessRequest>
{
    public ProcessValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required");
    }
}
