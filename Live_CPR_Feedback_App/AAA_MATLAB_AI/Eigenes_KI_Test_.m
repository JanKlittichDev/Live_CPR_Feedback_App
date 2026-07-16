
clear; clc;

load('Meine_CPR_Modelle_2.mat'); % Trainierte KI muss vorliegen
disp('Lese Test-Datei ein');
% Trage hier den Pfad zu der Datei ein
test_file = ""; 

wahre_Tiefe = categorical({'Hohe Tiefe'}); % Label vordefinieren für Tiefe
wahre_Rate  = categorical({'Schnelle Rate'}); % Label vordefinieren für Frequenz

data = readmatrix(test_file, "Delimiter", ";", "DecimalSeparator", ","); % Einlesen der Datei
acc = data(:,4); 
t = data(:,5);

Fs = 100;
acc = acc - mean(acc); % Entfernen von Offset

% Zerschneiden (Sliding Window)
fenster = 2 * Fs;
schrittweite = 0.5 * Fs; % Data-Augmentation
Anzahlwerte = length(acc);
Anzahlfenster = round((Anzahlwerte - fenster) / schrittweite); % Bestimmung der maximalen Fensteranzahl des Datensatzes

XTest = cell(Anzahlfenster, 1);
YTest_Tiefe_Wahr = repmat(wahre_Tiefe, Anzahlfenster, 1);
YTest_Rate_Wahr  = repmat(wahre_Rate, Anzahlfenster, 1);

for i = 1:Anzahlfenster
    startIndex = (i - 1) * schrittweite + 1;
    endIndex   = startIndex + fenster - 1;
    
    % Ausschneiden und als 1xN Vektor ablegen
    XTest{i} = reshape(acc(startIndex:endIndex), 1, []); % Ablegen in die Zellen für die Sammlung der Trainingsdaten
end

disp(' Lasse KI klassifizieren');

% classify() nimmt unsere Fenster und spuckt die Vorhersagen aus
Vorhersage_Tiefe = classify(trainedNet_Tiefe, XTest); % Klassifizierung
Vorhersage_Rate  = classify(trainedNet_Rate, XTest); % Klassifizierung

%Auswertung 

disp(' Berechne Genauigkeit');

Genauigkeit_Tiefe = sum(Vorhersage_Tiefe == YTest_Tiefe_Wahr) / numel(YTest_Tiefe_Wahr) * 100; % berechnen der Genauigkeiten in dem die zugewiesenen Label mit den wahren labels vergleicht werden.
Genauigkeit_Rate  = sum(Vorhersage_Rate == YTest_Rate_Wahr) / numel(YTest_Rate_Wahr) * 100;

disp(['-> Genauigkeit Tiefe: ', num2str(Genauigkeit_Tiefe), ' %']);
disp(['-> Genauigkeit Rate:  ', num2str(Genauigkeit_Rate), ' %']);

% Confusion Chart zeichnen
figure('Name', 'Ergebnisse der KI-Klassifizierung', 'Position', [100, 100, 800, 400]);

subplot(1,2,1);
confusionchart(YTest_Tiefe_Wahr, Vorhersage_Tiefe, ...
    'Title', ['Tiefen-KI (', num2str(round(Genauigkeit_Tiefe)), '%)'], ...
    'RowSummary','row-normalized');

subplot(1,2,2);
confusionchart(YTest_Rate_Wahr, Vorhersage_Rate, ...
    'Title', ['Raten-KI (', num2str(round(Genauigkeit_Rate)), '%)'], ...
    'RowSummary','row-normalized');