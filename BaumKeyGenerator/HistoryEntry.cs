namespace BaumKeyGenerator;

public record HistoryEntry(
    DateTime GeneratedAt,
    string   KeyType,
    string   Purpose,
    string   Value        // full value stored (encrypted at-rest in the store)
);
