using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    public sealed partial class UguiInventoryPresenter
    {
        bool showCharacter,characterWasShown;
        bool compactAttributes;
        readonly TMPro.TextMeshProUGUI[] compactAttributeLabels=new TMPro.TextMeshProUGUI[5];
        UnityEngine.UI.Button expeditionCharacterSection;
        void SwitchCharacterSection()
        { compactAttributes=!compactAttributes; selectionSlot=selectedBag=-1; equipmentNotice=null; respecConfirmation=false; Cancel(); dirty=true; }
        void InitializeProgression()
        {
            view.CharacterToggle.onClick.AddListener(()=>
            {
                if(UguiBaseHub.Active && UguiBaseHub.Compact && showCharacter)
                { SwitchCharacterSection(); }
                else { if(showCommerce) SetCommerce(false); showCharacter=!showCharacter; }
                respecConfirmation=false; Cancel(); dirty=true;
            });
            for(int i=0;i<view.AttributeButtons.Length;i++)
            {
                int id=i;
                view.AttributeButtons[i].onClick.AddListener(()=>
                {
                    if(CampController.Active) CampSession.Act(new CampAction { Kind=CampActionKind.InvestAttribute,Result=id });
                });
            }
        }
        void ProgressIntChanged(int before,int after)=>Changed();
        void ProgressLongChanged(long before,long after)=>Changed();
        void BindProgression(PlayerInventory target,bool bind)
        {
            if(target==null) return;
            if(bind)
            {
                target.CharacterLevel.OnValueChanged+=ProgressIntChanged; target.AvailableAttributePoints.OnValueChanged+=ProgressIntChanged;
                target.ConfirmedExperience.OnValueChanged+=ProgressLongChanged; target.PendingExperience.OnValueChanged+=ProgressLongChanged;
                target.LastConfirmedExperience.OnValueChanged+=ProgressLongChanged;
            }
            else
            {
                target.CharacterLevel.OnValueChanged-=ProgressIntChanged; target.AvailableAttributePoints.OnValueChanged-=ProgressIntChanged;
                target.ConfirmedExperience.OnValueChanged-=ProgressLongChanged; target.PendingExperience.OnValueChanged-=ProgressLongChanged;
                target.LastConfirmedExperience.OnValueChanged-=ProgressLongChanged;
            }
        }
        bool UpdateCharacterSheet()
        {
            if(UguiBaseHub.Compact) return UpdateCompactCharacterSheet();
            var bounds=InventoryWindow.Bounds; float left=bounds.width*.31f,middle=bounds.width*.32f,right=bounds.width-left-middle;
            view.Heading.gameObject.SetActive(false); view.Body.gameObject.SetActive(true);
            UguiGeometry.Place(view.CharacterHeading.rectTransform,new Rect(18,10,left+middle-36,42));
            var character=CampSession.Progress.Character; bool camp=CampController.Active || player==null;
            int level=camp ? character.Level : player.CharacterLevel.Value,points=camp ? character.UnspentPoints : player.AvailableAttributePoints.Value;
            long xp=camp ? character.Experience : player.ConfirmedExperience.Value;
            view.CharacterHeading.text="PERSONAGEM · NÍVEL "+level+"   ·   "+points+" pontos disponíveis";
            view.CharacterHeading.fontSize=23; view.CharacterHeading.enableAutoSizing=true; view.CharacterHeading.fontSizeMin=18; view.CharacterHeading.fontSizeMax=23;
            float size=Mathf.Min((left-34)/3,(bounds.height-174)/4),origin=(left-size*3)/2;
            int[] columns={1,1,0,1,2,2,0,2},rows={0,1,1,2,0,2,3,3};
            for(int i=0;i<view.EquipmentSlots.Length;i++)
            {
                var slot=view.EquipmentSlots[i]; slot.gameObject.SetActive(true);
                UguiGeometry.Place((RectTransform)slot.transform,new Rect(origin+columns[i]*size,62+rows[i]*size,size-6,size-6));
                slot.PresentEquipment(window.Equipped(i),i);
            }
            string[] names={"Força","Destreza","Vigor","Percepção","Vitalidade"};
            var body=new System.Text.StringBuilder();
            for(int i=0;i<5;i++)
            {
                var id=(AttributeId)i; long total=PresentedTotal(id);
                body.Append(names[i]).Append("   ").Append(CharacterAttributeRules.Effective(total)).Append('\n');
                body.Append("Base ").Append(PresentedBase(id)); if(total!=PresentedBase(id)) body.Append(" + equipamento ").Append(total-PresentedBase(id));
                body.Append('\n');
                var button=view.AttributeButtons[i];
                TradeButton(button,new Rect(left+middle-60,62+i*51,48,48),"+",camp && TradeReady && points>0 && character.Invested.Get(id)<CharacterAttributeRules.MaxInvestment);
            }
            UguiGeometry.Place(view.CharacterBody.rectTransform,new Rect(left+8,62,middle-78,268));
            view.CharacterBody.text=body.ToString(); view.CharacterBody.fontSize=18; view.CharacterBody.lineSpacing=6; view.CharacterBody.enableAutoSizing=false; view.CharacterBody.alignment=TMPro.TextAlignmentOptions.TopLeft;
            view.Body.text="MOCHILA · 12"; UguiGeometry.Place(view.Body.rectTransform,new Rect(left+middle+12,14,right-24,40));
            GridLayout(0,new Rect(left+middle+12,66,right-24,bounds.height-170),3,true); GridLayout(1,default,4,false);
            view.RespecButton.gameObject.SetActive(camp);
            if(respecRevision!=CampSession.Progress.Revision) respecConfirmation=false;
            int price=CampTrade.RespecPrice(level);
            TradeButton(view.RespecButton,new Rect(bounds.width-212,bounds.height-68,188,48),(respecConfirmation ? "Confirmar · " : "Redistribuir · ")+price,TradeReady && character.UnspentPoints<character.Level-1 && character.Coins>=price);
            if(respecConfirmation) equipmentNotice="Redistribuir os pontos investidos por "+price+" moedas? Clique em Confirmar para concluir.";
            else if(selectionSlot<0) equipmentNotice="Proteção física: "+(PresentedPhysicalProtection()*100).ToString("0")+"% · "+(level>=20 ? "XP no limite" : "XP "+(xp-CharacterProgressionRules.LevelStart(level))+" / "+CharacterProgressionRules.NextLevelCost(level));
            return true;
        }
        bool UpdateCompactCharacterSheet()
        {
            var bounds=InventoryWindow.Bounds; float half=bounds.width/2;
            var character=CampSession.Progress.Character;
            bool camp=CampController.Active || player==null;
            int level=camp ? character.Level : player.CharacterLevel.Value,points=camp ? character.UnspentPoints : player.AvailableAttributePoints.Value;
            long xp=camp ? character.Experience : player.ConfirmedExperience.Value;
            view.CharacterHeading.text="PERSONAGEM · NÍVEL "+level+"\n"+points+" pontos disponíveis";
            ReadableText(view.CharacterHeading,28); view.CharacterHeading.font=UiFontAssets.Title;
            UguiGeometry.Place(view.CharacterHeading.rectTransform,new Rect(20,6,bounds.width-284,72));
            view.Heading.gameObject.SetActive(false);
            view.Body.gameObject.SetActive(!compactAttributes);
            view.CharacterBody.gameObject.SetActive(false);
            view.CharacterToggle.gameObject.SetActive(true);
            TradeButton(view.CharacterToggle,new Rect(bounds.width-244,8,224,64),compactAttributes ? "Ver equipamentos" : "Ver atributos",true);
            if(!UguiBaseHub.Active)
            {
                if(expeditionCharacterSection==null)
                {
                    expeditionCharacterSection=Instantiate(view.CharacterToggle,view.CharacterToggle.transform.parent);
                    expeditionCharacterSection.name="CharacterSection";
                    expeditionCharacterSection.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
                    expeditionCharacterSection.onClick.AddListener(SwitchCharacterSection);
                }
                expeditionCharacterSection.gameObject.SetActive(true);
                UguiGeometry.Place(view.CharacterHeading.rectTransform,new Rect(20,6,bounds.width-552,72));
                TradeButton(expeditionCharacterSection,new Rect(bounds.width-512,8,208,64),compactAttributes ? "Equipamentos" : "Ver atributos",true);
                TradeButton(view.CharacterToggle,new Rect(bounds.width-292,8,160,64),"Mochila",true);
            }
            foreach(var slot in view.EquipmentSlots) slot.gameObject.SetActive(!compactAttributes);
            if(!compactAttributes)
            {
                view.EquipmentHeading.gameObject.SetActive(true);
                view.EquipmentHeading.text="EQUIPAMENTOS";
                ReadableText(view.EquipmentHeading,26);
                UguiGeometry.Place(view.EquipmentHeading.rectTransform,new Rect(20,82,half-40,36));
                view.Body.text="MOCHILA · 12"; ReadableText(view.Body,26);
                UguiGeometry.Place(view.Body.rectTransform,new Rect(half+20,82,half-40,36));
                float available=bounds.height-220;
                float size=Mathf.Min((half-40)/4,available/2);
                float x=20+(half-40-size*4)/2,y=122+(available-size*2)/2;
                for(int i=0;i<view.EquipmentSlots.Length;i++)
                {
                    var slot=view.EquipmentSlots[i];
                    UguiGeometry.Place((RectTransform)slot.transform,new Rect(x+i%4*size,y+i/4*size,size-6,size-6));
                    slot.PresentEquipment(window.Equipped(i),i);
                }
                GridLayout(0,new Rect(half+20,122,half-40,available),3,true);
                GridLayout(1,default,4,false);
            }
            else
            {
                GridLayout(0,default,3,false); GridLayout(1,default,4,false);
                string[] names={"Força","Destreza","Vigor","Percepção","Vitalidade"};
                for(int i=0;i<5;i++)
                {
                    if(compactAttributeLabels[i]==null)
                    {
                        var label=Instantiate(view.CharacterBody,view.CharacterBody.transform.parent);
                        label.name="CompactAttribute"+i; label.raycastTarget=false;
                        compactAttributeLabels[i]=label;
                    }
                    float x=20+i/3*half,y=92+i%3*80;
                    var id=(AttributeId)i; long total=PresentedTotal(id); int basis=PresentedBase(id);
                    var text=compactAttributeLabels[i]; text.gameObject.SetActive(true);
                    ReadableText(text,24); text.lineSpacing=0;
                    text.text=names[i]+" "+CharacterAttributeRules.Effective(total)+"\nBase "+basis+(total!=basis ? " · Equip. "+(total-basis) : "");
                    UguiGeometry.Place(text.rectTransform,new Rect(x,y,camp ? half-124 : half-40,68));
                    TradeButton(view.AttributeButtons[i],new Rect(x+half-112,y,64,64),"+",camp && TradeReady && points>0 && character.Invested.Get(id)<CharacterAttributeRules.MaxInvestment);
                }
            }
            foreach(var button in view.AttributeButtons) button.gameObject.SetActive(compactAttributes && camp);
            if(respecRevision!=CampSession.Progress.Revision) respecConfirmation=false;
            int price=CampTrade.RespecPrice(level);
            TradeButton(view.RespecButton,new Rect(bounds.width-284,bounds.height-76,264,64),(respecConfirmation ? "Confirmar · " : "Redistribuir · ")+price,TradeReady && character.UnspentPoints<character.Level-1 && character.Coins>=price);
            if(respecConfirmation) equipmentNotice="Redistribuir os pontos investidos por "+price+" moedas? Confirme para concluir.";
            else if(selectionSlot<0) equipmentNotice="Proteção física: "+(PresentedPhysicalProtection()*100).ToString("0")+"% · "+(level>=20 ? "XP no limite" : "XP "+(xp-CharacterProgressionRules.LevelStart(level))+" / "+CharacterProgressionRules.NextLevelCost(level))+
                (!camp && player.PendingExperience.Value>0 ? " · XP pendente: "+player.PendingExperience.Value : "");
            return true;
        }
        static void ReadableText(TMPro.TextMeshProUGUI text,float size)
        {
            text.enableAutoSizing=false; text.fontSize=size; text.font=UiFontAssets.Regular;
            text.textWrappingMode=TMPro.TextWrappingModes.Normal;
            text.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            text.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
        }
    }
}
