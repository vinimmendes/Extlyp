using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    public sealed partial class UguiInventoryPresenter
    {
        UguiInventorySlot controllerSource;
        ItemStack controllerItem;
        int cancelledSelectionFrame=-1;
        UguiInventorySlot lastPointerSlot;
        ItemStack lastPointerItem;
        float lastPointerTime;
        Vector2 lastPointerPosition;
        public void PointerTap(UguiInventorySlot slot,UnityEngine.EventSystems.PointerEventData data)
        {
            if(slot==null || !window.SlotActionsAllowed || source!=null || UiScreenCoordinator.Top!=PlayerScreen.Inventory) return;
            window.CopyPresentation(pack,other);
            var item=PresentedItem(slot);
            bool twice=!showCommerce && lastPointerSlot==slot && !item.Empty && item.Equals(lastPointerItem) &&
                Time.unscaledTime-lastPointerTime<=.35f && Vector2.Distance(data.position,lastPointerPosition)<=32*UiLayout.Scale;
            if(Debug.isDebugBuild && !showCommerce && lastPointerSlot==slot && !item.Empty && item.Equals(lastPointerItem) && !twice)
                Debug.Log("[Equipamento] Segundo clique/toque fora da janela: "+((Time.unscaledTime-lastPointerTime)*1000).ToString("0")+" ms (limite 350 ms); deslocamento "+Vector2.Distance(data.position,lastPointerPosition).ToString("0")+" px.");
            lastPointerSlot=twice ? null : slot; lastPointerItem=item;
            lastPointerTime=Time.unscaledTime; lastPointerPosition=data.position;
            if(twice && QuickEquipment(slot,item)) return;
            Tap(slot);
        }
        bool QuickEquipment(UguiInventorySlot slot,ItemStack item)
        {
            if(showCommerce || slot.Grid!=0 && slot.Grid!=2 || item.Empty) return false;
            if(slot.Grid==0 && !ItemCatalog.Weapon(item.Item) && !ItemCatalog.Equipment(item.Item)) return false;
            ClearControllerSelection(); discardConfirmation=false;
            if(CampController.Active && !TradeReady || !CampController.Active && (player==null || !player.IsAlive || player.ChangingEquipment))
            { equipmentNotice="Aguarde para trocar o equipamento."; dirty=true; return true; }
            int bag=slot.Slot,target=slot.Slot;
            if(slot.Grid==2)
            {
                bag=System.Array.FindIndex(pack.Slots,stack=>stack.Empty);
                if(bag<0) { equipmentNotice="Mochila cheia: libere um espaço para desequipar."; dirty=true; return true; }
            }
            else
            {
                if(ItemCatalog.Weapon(item.Item))
                {
                    target=ComparisonSlot(item);
                }
                else target=(int)ItemCatalog.EquipmentSlotFor(item.Item)-1;
                if(!EquipmentLoadout.Accepts(target,item))
                { equipmentNotice="Este item não pode ser equipado."; dirty=true; return true; }
            }
            // Reuse the authoritative exchange, including its existing validation,
            // base acknowledgement and timed/cancellable expedition action.
            if(Debug.isDebugBuild) Debug.Log("[Equipamento] Duplo clique/toque reconhecido; solicitando troca "+(CampController.Active ? "na base." : "na expedicao (acao de 2 s)."));
            window.ExchangeEquipment(bag,target);
            selectedBag=selectionSlot=-1; selectedItem=selectionItem=default;
            equipmentNotice=CampController.Active ? "Aguardando confirmação do servidor…" : "Troca solicitada. Fique parado para concluir.";
            dirty=true; return true;
        }
        bool ControllerReady => window.SlotActionsAllowed && !showCommerce &&
            UiScreenCoordinator.InputScreen==PlayerScreen.Inventory && UiScreenCoordinator.Top==PlayerScreen.Inventory &&
            (!CampController.Active || CampSession.Synchronized && !CampSession.Pending);
        ItemStack PresentedItem(UguiInventorySlot slot) => slot.Grid==2 ? window.Equipped(slot.Slot) : (slot.Grid==0 ? pack : other).Slots[slot.Slot];
        void ClearControllerSelection()
        {
            if(controllerSource!=null) controllerSource.MarkSource(false);
            controllerSource=null; controllerItem=default;
        }
        public bool TryCancelSelection()
        {
            if(cancelledSelectionFrame==Time.frameCount) return true;
            if(respecConfirmation) { cancelledSelectionFrame=Time.frameCount; respecConfirmation=false; dirty=true; return true; }
            if(showCommerce && (tradeSlot>=0 || shopOffer>=0)) { cancelledSelectionFrame=Time.frameCount; ResetTrade(); dirty=true; return true; }
            if(discardConfirmation) { cancelledSelectionFrame=Time.frameCount; discardConfirmation=false; dirty=true; return true; }
            if(controllerSource==null && source==null) return false;
            cancelledSelectionFrame=Time.frameCount; Cancel(); equipmentNotice=null; dirty=true; return true;
        }
        public void Submit(UguiInventorySlot slot)
        {
            if(slot==null || !window.SlotActionsAllowed || UiScreenCoordinator.InputScreen!=PlayerScreen.Inventory || UiScreenCoordinator.Top!=PlayerScreen.Inventory) return;
            if(showCommerce) { SelectSale(slot); return; }
            if(!ControllerReady || source!=null) return;
            window.CopyPresentation(pack,other);
            if(controllerSource==null)
            {
                var item=PresentedItem(slot);
                if(item.Empty) { equipmentNotice="Selecione um item de origem."; dirty=true; return; }
                SelectItem(slot); controllerSource=slot; controllerItem=item; slot.MarkSource(true);
                selectedBag=-1; selectedItem=default;
                equipmentNotice=Describe(item)+" · Confirme o destino para mover/equipar. B/Esc cancela; X/□/Delete descarta.";
                if(slot.Grid==0)
                {
                    int comparison=ComparisonSlot(item);
                    if(comparison>=0) equipmentNotice+="\nEquipado: "+Describe(window.Equipped(comparison),false);
                }
                dirty=true; return;
            }
            if(controllerSource==slot) { Cancel(); equipmentNotice=null; dirty=true; return; }
            var origin=controllerSource;
            if(!PresentedItem(origin).Equals(controllerItem))
            { Cancel(); equipmentNotice="O item mudou; selecione novamente."; dirty=true; return; }
            if(origin.Grid==0 && slot.Grid==2 || origin.Grid==2 && slot.Grid==0)
            {
                int bag=origin.Grid==0 ? origin.Slot : slot.Slot;
                int equipment=origin.Grid==2 ? origin.Slot : slot.Slot;
                if(!EquipmentLoadout.Accepts(equipment,pack.Slots[bag]))
                { equipmentNotice="Destino incompatível; escolha outro slot."; dirty=true; return; }
                window.ExchangeEquipment(bag,equipment);
            }
            else if(origin.Grid!=2 && slot.Grid!=2) window.Transfer(origin.Grid,origin.Slot,slot.Grid,slot.Slot);
            else { equipmentNotice="Mova entre equipamento e mochila; use a mochila antes do baú."; dirty=true; return; }
            Cancel(); dirty=true;
        }
        void DiscardControllerSelection()
        {
            if(!ControllerReady || controllerSource==null) return;
            window.CopyPresentation(pack,other);
            if(!PresentedItem(controllerSource).Equals(controllerItem))
            { Cancel(); equipmentNotice="O item mudou; selecione novamente."; dirty=true; return; }
            if(controllerSource.Grid==2 || RecoveryRules.Bound(controllerItem))
            { equipmentNotice="Guarde o equipamento na mochila primeiro. Itens vinculados não podem ser descartados."; dirty=true; return; }
            SelectItem(controllerSource); AskDiscard();
            Cancel(); dirty=true;
        }
    }
}
