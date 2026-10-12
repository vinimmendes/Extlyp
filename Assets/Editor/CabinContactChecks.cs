using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CyberExtraction.Camp;

// Focused regression for wall-foot contact and the reported sprite proportions.
public static class CabinContactChecks
{
    static int passed;
    static void Check(bool ok,string label)
    { if(!ok) throw new Exception("CABIN CONTACT FAIL: "+label); passed++; }

    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/Builds/SkillsValidation/"))
            throw new InvalidOperationException("Use a copia isolada de validacao.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var mode=Physics2D.simulationMode;
        var root=new GameObject("CabinContactFixture");
        try
        {
            passed=0; Physics2D.simulationMode=SimulationMode2D.Script;
            new CabinScenery(root.transform);
            var actor=new GameObject("ContactActor"); actor.transform.SetParent(root.transform,false);
            var body=actor.AddComponent<Rigidbody2D>(); body.gravityScale=0; body.freezeRotation=true;
            actor.AddComponent<CircleCollider2D>().radius=CabinScenery.PlayerRadius;
            Vector2 Move(Vector2 start,Vector2 velocity)
            {
                body.position=start; body.linearVelocity=velocity; Physics2D.SyncTransforms();
                for(int i=0;i<100;i++) Physics2D.Simulate(.02f);
                return body.position;
            }
            // Expected contact coordinates come from the painted feet of the walls.
            var right=Move(new Vector2(-3,4),Vector2.right*4);
            Check(right.x>-1.5f && right.x<-1.3f,"Interior da parede direita bloqueia no pe da parede: "+right);
            var left=Move(new Vector2(-10,.9f),Vector2.left*4);
            Check(left.x>-12.5f && left.x<-12.3f,"Interior da parede esquerda: "+left);
            var back=Move(new Vector2(-6,5),Vector2.up*4);
            Check(back.y>7.05f && back.y<7.25f,"Parede de fundo bloqueia antes da faixa de madeira: "+back);
            var front=Move(new Vector2(-3,2),Vector2.down*4);
            Check(front.y>.5f && front.y<.7f,"Parede frontal pelo lado interior: "+front);
            var outside=Move(new Vector2(3,4),Vector2.left*4);
            Check(outside.x>1.15f && outside.x<1.4f,"Parede direita tambem bloqueia pelo exterior: "+outside);
            var door=Move(CabinScenery.Spawn,Vector2.up*4);
            Check(door.y>2.5f,"Entrada continua atravessavel com o personagem maior: "+door);
            var art=CabinArt.Place(root.transform,"Player",Vector2.zero,Vector2.one*CabinScenery.PlayerHeight,5);
            var renderer=art.GetComponent<SpriteRenderer>();
            Check(Mathf.Abs(art.transform.localScale.x-art.transform.localScale.y)<.0001f,"Sprite nao esticado");
            Check(renderer.bounds.size.y>1.8f && renderer.bounds.size.x<renderer.bounds.size.y,"Personagem maior preserva a silhueta");
            Debug.Log("CABIN CONTACT CHECKS PASS: "+passed+" checks; manual 2D physics and sprite bounds only.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); Physics2D.simulationMode=mode; }
    }
}
