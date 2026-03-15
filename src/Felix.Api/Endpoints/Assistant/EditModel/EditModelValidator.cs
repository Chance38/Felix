using FluentValidation;

namespace Felix.Api.Endpoints.Assistant.EditModel;

public class EditModelValidator : AbstractValidator<EditModelRequest>
{
    public EditModelValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required");
    }
}
