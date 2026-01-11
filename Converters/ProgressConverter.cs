using Avalonia.Data.Converters;
using System;

namespace ProxyChecker.Converters;

public class ProgressConverter
{
    public static readonly IValueConverter ProgressToOpacityConverter =
        new FuncValueConverter<double, double>(progress =>
        {
            const double MaxOpacity = 1;
            const double MinOpacity = 0.2;
            const double MaxProgress = 100;
            double range = MaxOpacity - MinOpacity;
            double opacity = MinOpacity + progress / MaxProgress * range;

            return Math.Clamp(opacity, MinOpacity, MaxOpacity);
        });
}
