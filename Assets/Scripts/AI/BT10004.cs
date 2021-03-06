namespace Mogo.AI.BT
{
	public sealed class BT10004 : Mogo.AI.BehaviorTreeRoot
	{
		private static BT10004 _instance = null;
		public static BT10004 Instance
		{
			get
			{
				if(_instance == null)
					_instance = new BT10004();

				return _instance;
			}
		}

		private BT10004()
		{
			{
				Mogo.AI.SelectorNode node1 = new Mogo.AI.SelectorNode();
				this.AddChild(node1);
				{
					Mogo.AI.SequenceNode node2 = new Mogo.AI.SequenceNode();
					node1.AddChild(node2);
					{
						Mogo.AI.SelectorNode node3 = new Mogo.AI.SelectorNode();
						node2.AddChild(node3);
						node3.AddChild(new Mogo.AI.AOI(0));
						{
							Mogo.AI.SequenceNode node5 = new Mogo.AI.SequenceNode();
							node3.AddChild(node5);
							{
								Mogo.AI.Not node6 = new Mogo.AI.Not();
								node5.AddChild(node6);
								node6.Proxy(new Mogo.AI.SelectAutoFightMovePoint());
							}
							{
								Mogo.AI.Not node8 = new Mogo.AI.Not();
								node5.AddChild(node8);
								node8.Proxy(new Mogo.AI.EnterRest(1000));
							}
						}
					}
					{
						Mogo.AI.SelectorNode node10 = new Mogo.AI.SelectorNode();
						node2.AddChild(node10);
						{
							Mogo.AI.SequenceNode node11 = new Mogo.AI.SequenceNode();
							node10.AddChild(node11);
							node11.AddChild(new Mogo.AI.InSkillCoolDown(5));
							node11.AddChild(new Mogo.AI.InSkillRange(5));
							node11.AddChild(new Mogo.AI.CastSpell(5,0));
							node11.AddChild(new Mogo.AI.EnterCD(0));
						}
						{
							Mogo.AI.SequenceNode node16 = new Mogo.AI.SequenceNode();
							node10.AddChild(node16);
							node16.AddChild(new Mogo.AI.InSkillCoolDown(6));
							node16.AddChild(new Mogo.AI.InSkillRange(6));
							node16.AddChild(new Mogo.AI.CastSpell(6,0));
							node16.AddChild(new Mogo.AI.EnterCD(0));
						}
						{
							Mogo.AI.SequenceNode node21 = new Mogo.AI.SequenceNode();
							node10.AddChild(node21);
							node21.AddChild(new Mogo.AI.IsAngerFull());
							node21.AddChild(new Mogo.AI.PowerFX());
						}
						{
							Mogo.AI.SequenceNode node24 = new Mogo.AI.SequenceNode();
							node10.AddChild(node24);
							node24.AddChild(new Mogo.AI.IsPowerFX());
							node24.AddChild(new Mogo.AI.InSkillRange(4));
							node24.AddChild(new Mogo.AI.InSkillCoolDown(4));
							node24.AddChild(new Mogo.AI.CastSpell(4,0));
							node24.AddChild(new Mogo.AI.EnterCD(0));
						}
						{
							Mogo.AI.SequenceNode node30 = new Mogo.AI.SequenceNode();
							node10.AddChild(node30);
							node30.AddChild(new Mogo.AI.InSkillCoolDown(1));
							node30.AddChild(new Mogo.AI.InSkillRange(1));
							node30.AddChild(new Mogo.AI.CastSpell(1,0));
							node30.AddChild(new Mogo.AI.EnterCD(0));
						}
						{
							Mogo.AI.SequenceNode node35 = new Mogo.AI.SequenceNode();
							node10.AddChild(node35);
							node35.AddChild(new Mogo.AI.ChooseCastPoint(1));
							node35.AddChild(new Mogo.AI.MoveTo());
						}
					}
				}
			}
		}
	}
}
