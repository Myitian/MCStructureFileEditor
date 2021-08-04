namespace MCStructureFileEditor
{
    public static class Licenses
    {
        private static readonly string Split1 = "==========================================\n";
        private static readonly string Split2 = "------------------------------------------\n";

        public static string GetLicense(string lib)
        {
            
            lib = lib.ToLower();
            switch (lib)
            {
                case "mcstructurefileeditor":
                    return Properties.Resources.LICENSE;
                case "fnbt":
                    return Properties.Resources.LICENSE_fNbt;
                case "jetbrains":
                case "jetbrains.annotations":
                case "jetbrains_annotations":
                    return Properties.Resources.LICENSE_JetBrains_Annotations;
                case "all":
                    return Split1 + "StructureNBT\n" + Split2 + Properties.Resources.LICENSE
                        + Split1 + "fNbt\n" + Split2 + Properties.Resources.LICENSE_fNbt
                        + Split1 + "JetBrains.Annotations\n" + Split2 + Properties.Resources.LICENSE_JetBrains_Annotations
                        + Split1;
                default:
                    return "lib = all / MCStructureFileEditor / fNbt / JetBrains.Annotations";
            }
        }
    }
}
