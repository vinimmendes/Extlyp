using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Camp;
using Text=TMPro.TextMeshProUGUI;
namespace CyberExtraction.UI
{
    public sealed partial class UguiGameHud
    {
        void RefreshEditor(bool edit)
        {
            editor.gameObject.SetActive(edit && toolsVisible);
            float u=HudMetrics.Unit,w=Mathf.Min(480*u,UiLayout.Width-24*u),h=(HudLayout.HasLegacy ? 276 : 220)*u;
            var panel=new Rect((UiLayout.Width-w)/2,(UiLayout.Height-h)/2,w,h);
            toolbarToggle.gameObject.SetActive(edit);
            Place((RectTransform)toolbarToggle.transform,toolsVisible ? new Rect(panel.xMax-52*u,panel.y+4*u,48*u,48*u) : new Rect(UiLayout.Width-60*u,12*u,48*u,48*u));
            toolbarToggle.GetComponentInChildren<Text>(true).text=toolsVisible ? "×" : "HUD";
            if(!edit) return;
            foreach(var button in editor.GetComponentsInChildren<Button>(true))
            { button.GetComponent<HudFrameGraphic>().ApplyTheme(); button.GetComponentInChildren<Text>(true).color=HudTheme.Text; }
            Place(editor,panel); editor.GetComponent<HudFrameGraphic>().ApplyTheme();
            Place(editorTitle.rectTransform,new Rect(12*u,6*u,w-76*u,40*u)); editorTitle.text="ORGANIZAR HUD · "+HudMetrics.ProfileName+"\n"+HudLayoutEditor.SelectedName; editorTitle.color=HudTheme.Text; editorTitle.fontSize=18*u;
            var selected=HudLayout.EditingControl(HudLayoutEditor.Selected);
            float scale=HudLayout.Placement(selected).Size.x/HudLayout.DefaultSize(selected).x;
            Place(editorSize.rectTransform,new Rect(12*u,60*u,110*u,32*u)); editorSize.text="Tamanho "+Mathf.RoundToInt(scale*100)+"%"; editorSize.color=HudTheme.Text; editorSize.fontSize=14*u;
            Place(sizeSlider.GetComponent<RectTransform>(),new Rect(128*u,52*u,w-140*u,48*u));
            var buttons=interfaceRoot.Organizer.Buttons;
            Place((RectTransform)buttons[0].transform,new Rect(12*u,108*u,48*u,48*u));
            Place((RectTransform)buttons[1].transform,new Rect(68*u,108*u,48*u,48*u));
            Place((RectTransform)buttons[2].transform,new Rect(124*u,108*u,w-136*u,48*u));
            float bw=(w-40*u)/3;
            for(int i=3;i<=5;i++) Place((RectTransform)buttons[i].transform,new Rect(12*u+(i-3)*(bw+8*u),164*u,bw,48*u));
            legacyLayoutButton.gameObject.SetActive(HudLayout.HasLegacy);
            Place((RectTransform)legacyLayoutButton.transform,new Rect(12*u,220*u,w-24*u,48*u));
            foreach(var button in editor.GetComponentsInChildren<Button>(true)) button.GetComponentInChildren<Text>(true).fontSize=14*u;
            sizeSlider.SetValueWithoutNotify(scale);
        }
    }
}
