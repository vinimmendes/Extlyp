using CyberExtraction.Items;
namespace CyberExtraction.Camp
{
    public static class CampTestResources
    {
        public static CampProgress Create()
        {
            var profile=new CampProgress(); Supply(profile); return profile;
        }
        public static void Supply(CampProgress profile)
        {
            profile.Chest=new InventoryGrid(); profile.Pack=InventoryGrid.Starter();
            profile.Chest.Add(new ItemStack(ItemId.Wood,40)); profile.Chest.Add(new ItemStack(ItemId.Nails,20));
            profile.Chest.Add(new ItemStack(ItemId.Metal,40)); profile.Chest.Add(new ItemStack(ItemId.Gunpowder,20));
            profile.Chest.Add(new ItemStack(ItemId.AmmoComponent,20)); profile.Chest.Add(new ItemStack(ItemId.Bandage,20));
            profile.Pack.Add(new ItemStack(ItemId.FishCommon,5)); profile.Pack.Add(new ItemStack(ItemId.FishRare,3)); profile.Pack.Add(new ItemStack(ItemId.FishLarge,3));
            profile.Pack.Add(new ItemStack(ItemId.Bandage,10));
            profile.Ground.Clear(); profile.Revision++; profile.Sync();
        }
    }
}
