using System;
using ExCSS;
using FluentValidation;
using Nop.Plugin.Seed.ProductSync.Models;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Seed.ProductSync.Validators;

public class ConfigurationModelValidator:BaseNopValidator<ConfigurationModel>
{
    public ConfigurationModelValidator()
    {
        RuleFor(x => x.InfigoUrl).Must((url)=>Uri.IsWellFormedUriString(url,UriKind.Absolute));
    }
}