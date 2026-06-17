# 🏥 ClinicManager

Nowoczesny system zarządzania przychodnią medyczną zaimplementowany w architekturze MVC z użyciem platformy **ASP.NET Core 10**. System optymalizuje codzienną pracę placówek medycznych, umożliwiając kompleksowe zarządzanie wizytami, personelem, dokumentacją medyczną oraz wyliczaniem kosztów.

## 👥 Autorzy
- **Tobiasz Sroka**
- **Brajan Robak**

---

## ✨ Główne funkcjonalności
- **Inteligentny kalendarz:** Umawianie wizyt z wbudowaną walidacją kolizji terminów (blokada podwójnego bookowania dla lekarzy i pacjentów).
- **Karty wizyty:** Pełna historia pacjenta, z możliwością przypisywania wykonywanych procedur, zabiegów, notatek klinicznych oraz recept.
- **Moduł finansowy:** Dynamiczne wyliczanie kosztów całkowitych na podstawie bazy leków i wykonanych zabiegów.
- **Raportowanie PDF:** Możliwość generowania estetycznych, zagregowanych raportów dla dyrekcji z wykorzystaniem biblioteki `QuestPDF`.
- **Zabezpieczone API i Logi:** Dostęp chroniony przez system `ASP.NET Core Identity`, a każde krytyczne działanie monitorowane jest za pomocą technologii `NLog`.

---

## 🔐 System Logowania i Role

Aplikacja wykorzystuje ścisły podział uprawnień bazujący na rolach systemowych. Dostęp do poszczególnych modułów jest przyznawany dynamicznie w zależności od przypisanej grupy.

Dostępne są 3 główne role w systemie:

### 1. 🛡️ Administrator (`Admin`)
Główny zarządca systemu, często dyrekcja placówki.
- **Domyślne logowanie:** `admin@clinic.com` / `Admin123!`
- **Uprawnienia:** Pełny dostęp do każdej funkcji systemu. 
- **Moduły:** Rejestracja, edycja bazy leków, podgląd wizyt dowolnego lekarza, pełne zestawienia finansowe i zarządzanie kontami personelu.

### 2. 👩‍💼 Rejestracja (`Rejestratorka`)
Pracownik obsługujący pacjentów bezpośrednio z recepcji lub telefonicznie.
- **Domyślne logowanie:** `rejestracja@clinic.com` / `Rejestracja123!`
- **Uprawnienia:** Organizacja i koordynacja kalendarza placówki.
- **Moduły:** Zakładanie nowych wizyt, tworzenie kont pacjentów, anulowanie terminów, dostęp do raportów kosztów. Brak wglądu w notatki medyczne.

### 3. 🩺 Lekarz (`Lekarz`)
Pracownik medyczny przyjmujący pacjentów w gabinecie.
- **Domyślne logowanie:** `lekarz@clinic.com` lub `nowak@clinic.com` lub `kowalski@clinic.com` / `Lekarz123!`
- **Uprawnienia:** Dostęp i modyfikowanie dokumentacji klinicznej wyłącznie przypisanych mu pacjentów.
- **Moduły:** Podgląd dziennego harmonogramu, wypełnianie kart wizyt pacjentów, wprowadzanie notatek klinicznych, wystawianie recept na leki oraz dodawanie wykonanych podczas spotkania procedur medycznych (np. USG).

---

## 🛠️ Architektura i Technologia
- **Backend:** C#, .NET 10 (ASP.NET Core MVC)
- **Baza danych:** Entity Framework Core + SQL Server (zoptymalizowana indeksami zapobiegającymi `Full Table Scan`)
- **Mapowanie obiektów:** Riok.Mapperly (bardzo wydajne mapowanie na poziomie kompilacji)
- **Wydruki PDF:** QuestPDF
- **Testy jednostkowe:** xUnit + biblioteka `Moq` + in-memory database
- **Testy wydajnościowe:** NBomber
- **CI/CD:** Wdrożone środowisko ciągłej integracji korzystające z GitHub Actions.
