using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

public static class Versioning
{
    public enum Semantic
    {
        [Display(Name = "major")] Major
        , [Display(Name = "minor")] Minor
        , [Display(Name = "patch")] Patch
    }

    public enum Stability
    {
        [Display(Name = "alpha")] Alpha
        , [Display(Name = "beta")] Beta
        , [Display(Name = "release")] Release
    }
}