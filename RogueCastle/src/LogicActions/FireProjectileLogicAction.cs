using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DS2DEngine;
using Microsoft.Xna.Framework;

namespace RogueCastle
{
    public class FireProjectileLogicAction : LogicAction
    {
        ProjectileManager m_projectileManager;
        ProjectileData m_data;

        public FireProjectileLogicAction(ProjectileManager projectileManager, ProjectileData data)
        {
            m_projectileManager = projectileManager;
            m_data = data.Clone();
        }

        public override void Execute()
        {
            if (ParentLogicSet != null && ParentLogicSet.IsActive == true)
            {
                EnemyObj enemy = ParentLogicSet.ParentGameObj as EnemyObj;
                // Coop: aimed projectiles were built at init with P1 as target; aim at the enemy's current target.
                GameObj storedTarget = m_data.Target;
                m_data.Target = ResolveTarget(storedTarget);
                ProjectileObj obj = m_projectileManager.FireProjectile(m_data);
                m_data.Target = storedTarget;

                base.Execute();
            }
        }

        public override object Clone()
        {
            return new FireProjectileLogicAction(m_projectileManager, m_data);
        }

        public override void Dispose()
        {
            if (IsDisposed == false)
            {
                // Done
                m_projectileManager = null;
                m_data = null;
                base.Dispose();
            }
        }
    }
}
