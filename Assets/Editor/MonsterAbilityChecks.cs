using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Enemies;

public static class MonsterAbilityChecks
{
    [Serializable] class Snapshot { public MonsterEffect[] Effects; }
    static int checks;
    static void Check(bool result,string reason)
    { if(!result) throw new InvalidOperationException(reason); checks++; }
    [MenuItem("Cyber Extraction/Verificar regras e visuais dos monstros")]
    public static void Run()
    {
        checks=0;
        foreach(MonsterEffectKind kind in Enum.GetValues(typeof(MonsterEffectKind)))
        {
            var effect=new MonsterEffect {Id=42,Kind=kind,From=new Vector2(1,2),To=new Vector2(3,4),Radius=2.3f,Start=12.25,End=20.25};
            string json=JsonUtility.ToJson(new Snapshot {Effects=new[]{effect}});
            var copy=JsonUtility.FromJson<Snapshot>(json);
            Check(copy.Effects!=null && copy.Effects.Length==1 && effect.Equals(copy.Effects[0]),"JSON perde campos do efeito: "+kind+" / "+json);
        }
        foreach(var kind in new[]{MonsterEffectKind.Web,MonsterEffectKind.AcidPool})
        {
            var sprite=MonsterHazardArt.Get(kind);
            Check(sprite!=null && sprite.bounds.size.x==1 && sprite.bounds.size.y==1,"Diametro visual incorreto: "+kind);
            var pixels=sprite.texture.GetPixels(); int visible=0;
            foreach(var pixel in pixels) if(pixel.a>.5f) visible++;
            Check(visible>pixels.Length*.06f,"Efeito sem contraste suficiente: "+kind);
            Check(pixels[0].a==0,"Fundo do efeito precisa ser transparente");
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/monster-"+kind+".png",sprite.texture.EncodeToPNG());
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster.prefab");
        Check(prefab!=null,"Prefab do monstro ausente");
        var monster=prefab.GetComponent<Monster>();
        Check(Mathf.Approximately(monster.chargeSpeed,38),"Charger deve avancar a 38 unidades/s");
        Check(monster.chargeAimSeconds==3 && monster.chargeDuration==.5f && monster.chargeDamage==40,"Outras regras da investida alteradas");
        Check(PlantRules.CanSpawn(2,1,PlantKind.AttackBulb) && !PlantRules.CanSpawn(3,1,PlantKind.AttackBulb),"Limite de bulbos atacantes");
        Check(PlantRules.CanHeal(true,100,300,5,true) && !PlantRules.CanHeal(true,100,300,5,false),"Cura da mae ignora parede");
        Check(SporeRules.DamageThreshold(100)==25 && !SporeRules.Harmful(39,true) && SporeRules.Harmful(40,true),"Limiares dos esporos");
        File.WriteAllText("Logs/monster-ability-checks.json","{\"passed\":"+checks+",\"scope\":\"JSON effects, hazard art, charger prefab, plant and spore rules\"}");
        Debug.Log("MONSTER ABILITY CHECKS PASS: "+checks);
    }
}
