using System;
using System.Text;
using TMPro;
using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;

namespace CyberExtraction.UI
{
    // Binds the authored profile prefab to the owning account snapshot.
    public sealed class UguiProfilePresenter : MonoBehaviour
    {
        RectTransform panel;
        BaseHubView view;
        TMP_InputField input;
        TextMeshProUGUI title,hint,favorite,body;
        UnityEngine.UI.Button save;
        UnityEngine.UI.Button[] sections;
        UnityEngine.UI.Image weaponIcon;
        UnityEngine.UI.ScrollRect scroll;
        int section;
        string boundName;
        bool shown;
        long nextRename;
        public void Initialize(BaseHubView screen)
        {
            view=screen;
            var root=Instantiate(Resources.Load<GameObject>("UI/Prefabs/PlayerProfile"),view.Panel);
            var bindings=root.GetComponent<PlayerProfileView>();
            panel=bindings.Panel; title=bindings.Title; hint=bindings.Hint; favorite=bindings.Favorite;
            body=bindings.Body; input=bindings.NameInput; save=bindings.Save;
            sections=bindings.Sections; weaponIcon=bindings.WeaponIcon; scroll=bindings.Scroll;
            save.onClick.AddListener(Save);
            input.onValueChanged.AddListener(_=>UpdateNameHint());
            for(int i=0;i<sections.Length;i++)
            {
                int index=i;
                sections[i].onClick.AddListener(()=> { section=index; Refresh(); scroll.verticalNormalizedPosition=1; });
            }
            BaseMenuStyle.Scope(root,true); root.SetActive(false);
        }
        public void Show(bool visible)
        {
            if(panel==null) return;
            if(visible==shown) return;
            shown=visible; panel.gameObject.SetActive(visible);
            if(visible) { Layout(); Refresh(); }
        }
        public void Layout()
        {
            if(panel==null) return;
            var bounds=UguiBaseHub.ContentBounds; UguiGeometry.Place(panel,bounds); float w=bounds.width;
            UguiGeometry.Place(title.rectTransform,new Rect(16,8,w*.36f-24,48));
            float x=w*.36f,fieldWidth=w*.40f-12;
            UguiGeometry.Place((RectTransform)input.transform,new Rect(x,8,fieldWidth,48));
            UguiGeometry.Place(input.textViewport,new Rect(10,3,fieldWidth-20,42));
            UguiGeometry.Place(input.textComponent.rectTransform,new Rect(0,0,fieldWidth-20,42));
            UguiGeometry.Place(((TextMeshProUGUI)input.placeholder).rectTransform,new Rect(0,0,fieldWidth-20,42));
            UguiGeometry.Place((RectTransform)save.transform,new Rect(w*.76f,8,w*.24f-16,48));
            UguiGeometry.Place(hint.rectTransform,new Rect(16,58,w-32,38));
            for(int i=0;i<sections.Length;i++) UguiGeometry.Place((RectTransform)sections[i].transform,new Rect(16+i*(w-32)/5,100,(w-32)/5-4,48));
            UguiGeometry.Place(weaponIcon.rectTransform,new Rect(20,154,52,52));
            UguiGeometry.Place(favorite.rectTransform,new Rect(84,154,w-100,52));
            UguiGeometry.Place((RectTransform)scroll.transform,new Rect(16,214,w-32,bounds.height-226));
            UguiGeometry.Place(scroll.viewport,new Rect(0,0,w-32,bounds.height-226));
            LayoutBody();
        }
        void LayoutBody()
        {
            float w=UguiBaseHub.ContentBounds.width-44;
            float position=scroll.verticalNormalizedPosition;
            float height=Mathf.Max(1,body.GetPreferredValues(body.text,w-8,float.PositiveInfinity).y+16);
            UguiGeometry.Place(scroll.content,new Rect(0,0,w,height));
            UguiGeometry.Place(body.rectTransform,new Rect(4,0,w-8,height));
            scroll.verticalNormalizedPosition=position;
        }
        void Save()
        {
            if(!CampSession.Synchronized || CampSession.Pending) return;
            if(!PlayerNameRules.TryNormalize(input.text,out var normalized,out var error)) { hint.text=error; return; }
            input.DeactivateInputField();
            CampSession.Act(new CampAction {Kind=CampActionKind.SetName,Name=normalized});
            Refresh();
        }
        void UpdateNameHint()
        {
            if(!PlayerNameRules.TryNormalize(input.text,out _,out var error)) hint.text=error;
            else hint.text="Primeira escolha grátis. Depois, uma alteração gratuita a cada 7 dias.";
        }
        void Update()
        {
            if(shown && nextRename>0 && DateTime.UtcNow.Ticks>=nextRename) { nextRename=0; Refresh(); }
        }
        public void Refresh()
        {
            if(panel==null) return;
            BaseMenuStyle.Scope(panel.gameObject,true);
            var p=CampSession.Progress; var s=p.Statistics;
            title.text=string.IsNullOrEmpty(p.PlayerName) ? "SEU PERFIL" : p.PlayerName;
            if(boundName!=p.PlayerName) {boundName=p.PlayerName; input.SetTextWithoutNotify(p.PlayerName ?? "");}
            bool allowed=string.IsNullOrEmpty(p.PlayerName) || DateTime.UtcNow.Ticks-p.NameChangedTicks>=PlayerNameRules.RenameDelay;
            nextRename=allowed ? 0 : p.NameChangedTicks+PlayerNameRules.RenameDelay;
            input.interactable=save.interactable=CampSession.Synchronized && !CampSession.Pending && allowed;
            UpdateNameHint();
            if(!allowed) hint.text="Próxima alteração de nome: "+new DateTime(nextRename,DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            if(CampSession.Pending) hint.text="Aguardando confirmação do servidor…";
            input.textComponent.color=HudTheme.Text;
            ((TextMeshProUGUI)input.placeholder).color=BaseMenuStyle.Secondary;
            UguiMenuStyle.Button(save);
            var saveLabel=save.GetComponentInChildren<TextMeshProUGUI>(true);
            saveLabel.enableAutoSizing=true; saveLabel.fontSizeMin=18; saveLabel.fontSizeMax=21;
            foreach(var button in sections)
            {
                UguiMenuStyle.Button(button);
                var label=button.GetComponentInChildren<TextMeshProUGUI>(true); label.fontSize=19; label.richText=false;
                label.color=HudTheme.Text;
                var frame=button.GetComponent<HudFrameGraphic>(); frame.BaseSelected=button==sections[section]; frame.SetVerticesDirty();
            }
            title.color=favorite.color=body.color=HudTheme.Text; hint.color=BaseMenuStyle.Secondary;
            var preferred=s?.Favorite;
            weaponIcon.gameObject.SetActive(preferred!=null);
            weaponIcon.sprite=preferred!=null ? ItemIconLibrary.Get(preferred.Weapon) : null;
            favorite.text="ARMA PREFERIDA · "+(preferred==null ? "Ainda não definida" : ItemCatalog.Name(preferred.Weapon)+" · "+preferred.Kills+" abates");
            body.text=!CampSession.Synchronized ? "Sincronizando perfil…" : s==null ? "Estatísticas indisponíveis." : Build(p,section);
            LayoutBody();
        }
        static string Duration(double seconds)=>TimeSpan.FromSeconds(seconds).TotalHours>=1 ? (long)(seconds/3600)+"h "+(long)(seconds%3600/60)+"min" : (long)(seconds/60)+"min "+(long)(seconds%60)+"s";
        public static string SpeciesName(ProfileSpecies species)=>species switch
        {
            ProfileSpecies.Human=>"Jogador",ProfileSpecies.Bot=>"Bot",ProfileSpecies.Environment=>"Ambiente",
            ProfileSpecies.Charger=>"Investidor",ProfileSpecies.Listener=>"Ouvinte",ProfileSpecies.Shell=>"Couraçado",ProfileSpecies.Weaver=>"Aranha",
            ProfileSpecies.Herald=>"Arauto",ProfileSpecies.Mimic=>"Mímico",ProfileSpecies.Acid=>"Corrosivo",ProfileSpecies.Stalker=>"Espreitador",
            ProfileSpecies.Devourer=>"Devorador",ProfileSpecies.Spore=>"Esporífero",ProfileSpecies.PlantMother=>"Planta-mãe",
            ProfileSpecies.AttackBulb=>"Bulbo de ataque",ProfileSpecies.HealingBulb=>"Bulbo de cura",ProfileSpecies.PlantMinion=>"Servo vegetal",_=>"Desconhecido"
        };
        static string Outcome(ExpeditionOutcome value)=>value switch
        { ExpeditionOutcome.Extracted=>"Saiu com vida",ExpeditionOutcome.Dead=>"Morreu",ExpeditionOutcome.TimeUp=>"Tempo esgotado",ExpeditionOutcome.Interrupted=>"Interrompida",_=>"Em andamento" };
        static string Build(CampProgress p,int section)
        {
            var s=p.Statistics; var b=new StringBuilder();
            string rate=s.Ended==0 ? "—" : (100d*s.Extracted/s.Ended).ToString("0.0")+"%";
            if(section==0)
            {
                var c=p.Character; b.AppendLine("NÍVEL "+c.Level+" · XP "+c.Experience+" / "+(c.Level>=CharacterProgressionRules.MaxLevel ? "máximo" : CharacterProgressionRules.LevelStart(c.Level+1).ToString()));
                b.AppendLine("Extrações com vida: "+s.Extracted+" · Taxa de extração: "+rate+" · Mortes: "+s.Deaths);
                b.AppendLine("Criaturas eliminadas: "+s.CreatureKills+" · Jogadores: "+s.HumanKills+" · Bots: "+s.BotKills);
                b.AppendLine("Sequência atual: "+s.Streak+" · Recorde de extrações seguidas: "+s.BestStreak);
                b.AppendLine("Tempo em expedição: "+Duration(s.Seconds));
                b.Append("Estatísticas desde "+(s.SinceTicks>0 ? new DateTime(s.SinceTicks,DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "esta atualização")+". Histórico anterior não foi reconstruído.");
            }
            else if(section==1)
            {
                b.AppendLine("Iniciadas: "+s.Started+" · Encerradas: "+s.Ended+" · Em andamento: "+(s.Current==null ? 0 : 1));
                b.AppendLine("Saiu com vida: "+s.Extracted+" · Mortes: "+s.Deaths+" · Tempo esgotado: "+s.TimeUps+" · Interrupções: "+s.Interrupted);
                b.AppendLine("Taxa de extração: "+rate+" ("+s.Extracted+" / "+s.Ended+", incluindo interrupções)");
                double active=s.Current?.Seconds ?? 0;
                b.AppendLine("Tempo total: "+Duration(s.Seconds)+" · Média encerradas: "+(s.Ended==0 ? "—" : Duration((s.Seconds-active)/s.Ended))+" · Maior duração: "+Duration(s.LongestSeconds));
                foreach(var map in s.Maps) b.AppendLine(MapBuilder.MapName((ExpeditionMap)map.Map)+": "+map.Started+" iniciadas · "+map.Extracted+" extrações");
            }
            else if(section==2)
            {
                b.AppendLine("Abates · Criaturas: "+s.CreatureKills+" · Jogadores: "+s.HumanKills+" · Bots: "+s.BotKills);
                b.AppendLine("Assistências: "+s.Assists+" · Participações: "+(s.CreatureKills+s.HumanKills+s.BotKills+s.Assists));
                b.AppendLine("Dano causado · Criaturas: "+s.DamageCreatures+" · Participantes: "+s.DamageParticipants);
                b.AppendLine("Dano recebido: "+s.DamageReceived+" · Cura recebida: "+s.Healing);
                b.AppendLine("XP obtido em combate: "+s.CombatXp+" · XP confirmado nas expedições: "+s.ConfirmedXp);
                foreach(var row in s.Species) if(row.Deaths>0) b.AppendLine("Mortes por "+SpeciesName(row.Species)+": "+row.Deaths);
                foreach(var row in s.Weapons) b.AppendLine(ItemCatalog.Name(row.Weapon)+": "+row.Kills+" abates");
            }
            else if(section==3)
            {
                foreach(ProfileSpecies species in Enum.GetValues(typeof(ProfileSpecies)))
                {
                    if(species<ProfileSpecies.Charger) continue;
                    var row=s.Species.Find(r=>r.Species==species);
                    if(row==null || !row.HasActivity) continue;
                    long kills=row.Kills,assists=row.Assists;
                    b.AppendLine(SpeciesName(species)+" · Abates "+kills+" · Assistências "+assists+" · Participações "+(kills+assists)+" · Mortes sofridas "+(row?.Deaths ?? 0));
                }
                if(b.Length==0) b.AppendLine("Nenhuma criatura registrada em expedições normais. Testes e treino não entram nesta contagem.");
            }
            else
            {
                if(s.History.Count==0) b.AppendLine("Nenhuma expedição encerrada desde o início da coleta.");
                foreach(var run in s.History)
                {
                    b.AppendLine(new DateTime(run.StartedTicks,DateTimeKind.Utc).ToLocalTime().ToString("dd/MM HH:mm")+" · "+MapBuilder.MapName((ExpeditionMap)run.Map)+" · "+Outcome(run.Outcome)+" · "+Duration(run.Seconds));
                    b.AppendLine("Criaturas "+run.CreatureKills+" · Jogadores "+run.HumanKills+" · Bots "+run.BotKills+" · Assistências "+run.Assists+" · XP confirmado "+run.ConfirmedXp);
                    b.AppendLine("Dano em criaturas "+run.DamageCreatures+" · Em participantes "+run.DamageParticipants+" · Recebido "+run.DamageReceived+" · Cura "+run.Healing);
                    if(run.Outcome==ExpeditionOutcome.Extracted && run.Exit>=0)
                        b.AppendLine("Saída: "+(run.Map==(byte)ExpeditionMap.Dungeon && run.Exit<DungeonLayout.ExitNames.Length ? DungeonLayout.ExitNames[run.Exit] : "Extração "+(run.Exit+1)));
                    if(run.Outcome==ExpeditionOutcome.Dead) b.AppendLine("Morto por: "+(run.DeathSource==ProfileSpecies.Human || run.DeathSource==ProfileSpecies.Bot ? run.KillerName ?? SpeciesName(run.DeathSource) : SpeciesName(run.DeathSource)));
                    b.AppendLine();
                }
            }
            return b.ToString();
        }
    }
}
