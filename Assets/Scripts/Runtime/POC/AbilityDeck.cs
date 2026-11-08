using System;
using System.Collections.Generic;
using System.Linq;

public sealed class AbilityDeck
{
    public readonly List<string> cards = new List<string>();

    public AbilityDeck(IEnumerable<string> _ids, int _sets, int _seed)
    {
        string[] ids = _ids.OrderBy(_id => _id, StringComparer.Ordinal).ToArray();
        if (ids.Length != 20 || ids.Distinct().Count() != 20)
        {
            throw new ArgumentException("The deck requires twenty unique abilities.");
        }
        var random = new Random(_seed);
        for (int set = 0; set < _sets; set++)
        {
            var batch = (string[])ids.Clone();
            for (int i = batch.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (batch[i], batch[j]) = (batch[j], batch[i]);
            }
            cards.AddRange(batch);
        }
    }
}
