using EnumsNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public enum GitVerTag
{
    [Display(Name = "major")] Major
    , [Display(Name = "minor")] Minor
    , [Display(Name = "patch")] Patch
    , [Display(Name = "alpha", GroupName = GitVerTagExtension.PreTag)] Alpha
    , [Display(Name = "beta", GroupName = GitVerTagExtension.PreTag)] Beta
    , [Display(Name = "release")] Release
}

public static class GitVerTagExtension
{
    public const string PreTag = "pre-tag";

    public static IReadOnlyCollection<DisplayAttribute> GetPreTagDisplayAttributes()
    {
        var preTagDisplayAttributes = Enums.GetMembers<GitVerTag>()
                .Select(gitVerTag => gitVerTag.Attributes.Get<DisplayAttribute>())
                .Where(gitVerTagDisplayAttribute => gitVerTagDisplayAttribute
                    .GroupName == GitVerTagExtension.PreTag)
                .ToList()
                .AsReadOnly();

        return preTagDisplayAttributes;
    }

    public static bool ContainsPreTag(string gitTag)
    {
        var preTagDisplayAttributes = GetPreTagDisplayAttributes();
        var doesContainPreTag = preTagDisplayAttributes
                .Any(displayAttribute => gitTag.Contains(displayAttribute.Name));

        return doesContainPreTag;
    }

    public static bool IsBumpVersionValid(string bumpVersion)
    {
        if(String.IsNullOrWhiteSpace(bumpVersion))
        {
            return false;
        }

        var isValid = Enum.GetValues<GitVerTag>()
                .Any(gitVerTag => String.Equals(
                    bumpVersion
                    , gitVerTag.AsString<GitVerTag>()
                    , StringComparison.InvariantCultureIgnoreCase));

        return isValid;
    }
}