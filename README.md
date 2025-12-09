# IMR_Project

Team:  
Chelba Sergiu-Mihai  
Maciuc Mihai  
Prodan Beatrice  
Răducanu Georgiana-Elena  

Link docs: 
https://docs.google.com/document/d/18CswSSak9ubVLHiX2vDzS4gzRvs4X7crPk8iOgCNPPM/edit?tab=t.0#heading=h.2dxm8k6maghn



Document Tema sapt 6: https://docs.google.com/document/d/1ABFwQM0qp4ObbANLvdh2IHrA0SMWZ_c0D2S95FQZMPQ/edit?tab=t.0#heading=h.bcldxwdefncd
Video decor: https://youtu.be/J5JMj5fXyJI


Saptamana 7:
am trecut spre un decor real, am importat scanarea holului si a trei laboratoare in unity, am modelat cateva obiecte de care vom avea nevoie pentru a realiza scena completa, am inceput sa lucram pe partea de LLM
Video lab 7: https://youtu.be/qHYOPWbqUEg

Device: 1WMHHA70F03314

Saptamana 10:
Avatar Creation Workflow
În această săptămână a fost realizat procesul de creare a avatarurilor digitale pentru proiect. Modelarea se face printr-un selfie, iar generarea avatarului complet este realizată automat cu ajutorul platformei Avaturn. Sistemul procesează fotografia, reconstruiește fața în 3D și generează un model complet riggat, pregătit pentru integrarea în Unity.

Speech-to-Text Research
În paralel, am început documentarea soluțiilor de speech-to-text pentru a permite interacțiuni vocale cu avatarurile. Analizăm mai multe opțiuni (locale și cloud), evaluând acuratețea, latența, compatibilitatea cu Unity și posibilitățile de integrare în fluxul de dialog AI.

links:
https://youtube.com/shorts/I1jImqBZ-p0?feature=share
https://youtube.com/shorts/gjijORfgksY?feature=share

Saptamana 11:
Acest update include finalizarea geometriei nivelului, implementarea sistemului de recunoaștere vocală (Speech-to-Text) și integrarea completă a unui LLM local pentru interacțiunea cu avatarul profesorului.

Modificări Principale
1. Environment & Geometrie
Optimizare Collidere: Au fost remediate problemele de coliziune pentru Hol și sălile de laborator (Amazon, Bitdefender, Gemini).
Procesare Mesh-uri: Ușile au fost decupate și procesate manual în Blender pentru a elimina barierele invizibile, permițând acum navigarea fluidă (intrare/ieșire) în toate sălile.

2. Sistem Voice-to-Text (Google API)
A fost implementat un sistem de recunoaștere vocală bazat pe Google Cloud Speech-to-Text.
Funcționalitate: Push-to-talk (ținând apăsată tasta SPACE).

Suport: Recunoaște limba română și transcrie automat textul în interfața de chat a avatarului.
Notă: Sistemul utilizează Free Tier-ul Google API (limitat la 60 min/lună).

 
3. Integrare AI Local (Avatar Profesor)
Script Refactoring: Scriptul de chat (DiscutieEric) a fost refăcut pentru portabilitate.
Conectivitate: S-a eliminat IP-ul hardcodat, trecând la utilizarea localhost (127.0.0.1). Acest lucru permite rularea proiectului pe orice mașină care are serverul AI pornit local, eliminând problemele de rețea.


link:
https://youtu.be/1upZ1ne0bUQ

