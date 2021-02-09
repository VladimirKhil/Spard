using System.Collections.Generic;

namespace Spard.Transitions
{
    internal sealed class ModificationsList : List<PlannedLinksModification>
    {
        public ModificationsList()
        {

        }

        public ModificationsList(IEnumerable<PlannedLinksModification> collection)
            : base(collection)
        {

        }

        public ModificationsList Clone() => new ModificationsList(this);
    }
}
