using FluentValidation;

namespace CqrsDemo.Api.Cqrs;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Workspace name is required");
        RuleFor(x => x.Name).MaximumLength(100).WithMessage("Name must be 100 chars or less");
    }
}
