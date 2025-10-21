using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace app_ointment_backend.Infrastructure;

public class TimeOnlyModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        if (TimeOnly.TryParse(value, out TimeOnly time))
        {
            bindingContext.Result = ModelBindingResult.Success(time);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(modelName, "Invalid time format. Please use HH:mm format.");
        }

        return Task.CompletedTask;
    }
}