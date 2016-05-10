# FLR EPJ Demopakke

## Innledning
Dette dokumentet beskriver innholdet i EPJ demopakke. Demopakken inneholder .NET eksempler på hvordan integrere seg med fastlegeregisterets webtjenester og service bus. Demopakken er til enhver tid tilgjengelig på GitHub (https://github.com/NorskHelsenett/FlrEpjDemo). 

## Oppsett
For å hente ned koden lokalt, enten bruk en Git-klient og kjør 
```
git clone https://github.com/NorskHelsenett/FlrEpjDemo
```
eller last ned som en .zip-fil via følgende url: https://github.com/NorskHelsenett/FlrEpjDemo/archive/master.zip. 
Etter at koden er lastet ned må submoduler initieres ved å kjøre 
```
git submodule update –init
```
i roten på ditt lokale repository.

## Struktur
Demopakken består av en .NET solution med fire .NET 4.6.1 prosjekter. Du finner solution-fil under src-mappen. To av prosjektene (NHN.DtoContracts og NHN.WcfClientFactory) kommer fra et annet GitHub-repository (https://github.com/NorskHelsenett/NHNDtoContracts) og er inkludert FlrEpjDemo-repositoryet som en git submodule. Her følger en beskrivelse av hvert enkelt prosjekt.

### NHN.DtoContract
Inneholder alle kontrakter og tjenestegrensesnitt for fastlegeregisteret.

### NHN.WcfClientFactory
Inneholder hjelpekode for å kalle WCF-tjenester uten å måtte sette opp XML-konfigurasjon.

### FlrEpjDemo.Lib
Inneholder generisk kode for bruk i eksempel-prosjekt.

### FlrEpjDemo.Console
Inneholder en konsollapplikasjon som demonstrerer ulike eksempler på uthentig av informasjon fra fastlegeregisteret samt abonnering på service bus og mottak av ulike hendelser. Disse eksemplene fins under mappen Examples. Før man kan kjøre denne applikasjon må nødvendig konfigurasjon inn i App.config:

Setting                               | Beskrivelse
--------------------------------------|------------
OrganizationNumber	                  | Ditt organisasjonsnummer.
WcfClientFactory.Username	            | Brukernavn til gyldig bruker i grunndata.
WcfClientFactory.Password	            | Passord til bruker.
Microsoft.ServiceBus.ConnectionString | Connection string til service bus. Må inkludere brukernavn og passord (samme som over).
SubscriptionInfo.EventSource	        | Hvilket register å oprette service bus abonnement for.
SubscriptionInfo.UserSystemIdent	    | Brukerdefinert identifikatorstring for ditt service bus abonnement.


## Eksempler
Disse eksemplene fins under mappen `Examples` i `FlrEpjDemo.Console`.

### GetPatientlists
Illustrerer hvordan hente fastlegeavtaler og fastlegelister for din organisasjon.

### ReceiveEvents
Illustrerer hvordan bruke `FlrEventManager` til å lytte på hendelser og behandle dem.

### CollectingEvents
Illustrerer hvordan bruke `FlrEventManager` til å lytte på hvilken som helst hendelse og samle de opp over tid.
