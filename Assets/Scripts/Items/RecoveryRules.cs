using UnityEngine;

namespace CyberExtraction.Items
{
    public static class RecoveryRules
    {
        public static bool Bound(ItemStack stack) => !stack.Empty && (stack.Instance.Origin==ItemOrigin.Recovery || stack.Instance.RecoveryRounds>0);
        public static bool Valid(ItemStack stack) => (byte)stack.Instance.Origin<=(byte)ItemOrigin.Recovery && stack.Instance.RecoveryRounds>=0 &&
            (stack.Instance.RecoveryRounds==0 || ItemCatalog.Weapon(stack.Item) && !ItemCatalog.Melee(stack.Item) && stack.Instance.RecoveryRounds<=stack.Data);
        public static ItemStack DeathDrop(ItemStack stack)
        {
            if(stack.Instance.Origin==ItemOrigin.Recovery) return default;
            stack.Data=Mathf.Max(0,stack.Data-stack.Instance.RecoveryRounds); stack.Instance.RecoveryRounds=0;
            return stack;
        }
        public static ItemStack Extracted(ItemStack stack)
        { stack.Instance.Origin=ItemOrigin.Normal; stack.Instance.RecoveryRounds=0; return stack; }
    }
}
