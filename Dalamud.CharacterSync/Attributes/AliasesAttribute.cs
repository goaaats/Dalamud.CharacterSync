using System;

namespace Dalamud.CharacterSync.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AliasesAttribute : Attribute
    {
        public string[] Aliases { get; }

        public AliasesAttribute(params string[] aliases)
        {
            Aliases = aliases;
        }
    }
}