namespace CyberExtraction.Items
{
    public static class WeaponSwitchTiming
    {
        public static double Duration(byte source,byte target)
        {
            if(source==target || target==255) return 0;
            if(ItemCatalog.Melee((ItemId)source) || ItemCatalog.Melee((ItemId)target)) return .45;
            return 0;
        }
    }
}
