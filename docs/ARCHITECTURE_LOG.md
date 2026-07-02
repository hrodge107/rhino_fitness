# Architecture Log

Durable record of Medium/High violations and explicit bypasses across sessions.
Prepend new entries to the top (most recent first). Do not rewrite existing entries.

---

## 2026-06-24 02:28 — Medium — Web / Integration Standard (Accepted Proxy Seam)
- **Files**: [ProxyUrlConverter.cs](file:///c:/Users/User/Desktop/College/MCL/YEAR%202/3T/IT123P/FinalProject/fitness_app/FitnessApp/Converters/ProxyUrlConverter.cs), [ExerciseCatalogPage.xaml](file:///c:/Users/User/Desktop/College/MCL/YEAR%202/3T/IT123P/FinalProject/fitness_app/FitnessApp/Views/ExerciseCatalogPage.xaml)
- **Issue**: The origin domain (`static.exercisedb.dev`) blocks non-browser HTTP user agents (like .NET MAUI / WinUI image loaders), causing exercise illustration image loads to fail silently and render blank cards.
- **Action taken**: Completed task by introducing `ProxyUrlConverter` to route affected external media URLs through the public `wsrv.nl` image proxy service. Since this logic resides entirely in a UI-specific value converter, it does not leak API or network routing details into the ViewModel or Model domains, thus fully respecting MVVM decoupling and Dependency Inversion.

---

## 2026-06-22 20:37 — Medium — User-Directed Bypass

- **Files**: c:/Users/User/Desktop/College/MCL/YEAR 2/3T/IT123P/FinalProject/fitness_app/FitnessApp/Views/ExerciseCatalogPage.xaml
- **Issue**: Exercise Card layout includes a visual placeholder image (`dotnet_bot.png`) due to lack of local or remote exercise illustrations in the seed data.
- **Action taken**: Completed task by referencing `dotnet_bot.png` as a temporary placeholder and flagged in walkthrough notes for Phase-2 resource integration, following explicit user-directed bypass.

## 2026-06-22 — Medium — DIP (accepted infrastructure boundary)

- **Files**: Services/IDatabaseService.cs, Services/ExerciseRepository.cs
- **Issue**: `IDatabaseService.Connection` exposes the concrete `SQLiteAsyncConnection` to `ExerciseRepository`, which then calls `Table<Exercise>()` on it directly. ViewModels remain clean (they depend on `IExerciseRepository`), but the repository layer depends on a concrete SQLite type rather than a pure abstraction.
- **Action taken**: Completed the offline-first pipeline as specified. This is the intended seam for sqlite-net-pcl — the repository pattern here is built *on top of* `SQLiteAsyncConnection`, not behind a second abstraction. Re-wrapping `Table<T>()` behind a custom `IAsyncQuerySession` would be a KISS/YAGNI violation for one caller. Noted for visibility; no change recommended unless a second data source (Supabase in Phase 2) actually forces the abstraction.
