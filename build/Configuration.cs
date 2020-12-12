// Copyright 2020 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE
// https://github.com/nuke-build/nuke/commit/af2bd97550d1c52a8cfa79fb5098a67911cd0145#diff-474a8869a56b1c787ce449d8e01c7a6430c5ed868c7e1ce728e44af7e27ef0c1
// https://github.com/nuke-build/nuke/issues/598

using System;
using System.ComponentModel;
using System.Linq;
using Nuke.Common.Tooling;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug = new Configuration { Value = nameof(Debug) };
    public static Configuration Release = new Configuration { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration)
    {
        return configuration.Value;
    }
}