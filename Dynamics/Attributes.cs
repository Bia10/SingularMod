#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: apocdevhb $
// $Date: 2012-09-15 20:28:15 +0000 (Sa, 15 Sep 2012) $
// $HeadURL: https://subversion.assembla.com/svn/singular_v3/trunk/Singular/Dynamics/Attributes.cs $
// $LastChangedBy: apocdevhb $
// $LastChangedDate: 2012-09-15 20:28:15 +0000 (Sa, 15 Sep 2012) $
// $LastChangedRevision: 771 $
// $Revision: 771 $

#endregion

using System;

using Singular.Managers;
using Styx;


namespace Singular.Dynamics
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    internal sealed class IgnoreBehaviorCountAttribute : Attribute
    {
        public IgnoreBehaviorCountAttribute(BehaviorType type)
        {
            Type = type;
        }

        public BehaviorType Type { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    internal sealed class BehaviorAttribute : Attribute
    {
        public BehaviorAttribute(BehaviorType type, WoWClass @class = WoWClass.None, WoWSpec spec =(WoWSpec) int.MaxValue, WoWContext context = WoWContext.All, int priority = 0)
        {
            Type = type;
            SpecificClass = @class;
            SpecificSpec = spec;
            SpecificContext = context;
            PriorityLevel = priority;
        }

        public BehaviorType Type { get; private set; }
        public WoWSpec SpecificSpec { get; private set; }
        public WoWContext SpecificContext { get; private set; }
        public WoWClass SpecificClass { get; private set; }
        public int PriorityLevel { get; private set; }
    }
}