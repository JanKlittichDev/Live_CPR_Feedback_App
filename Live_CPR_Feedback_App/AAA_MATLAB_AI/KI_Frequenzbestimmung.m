clear
clc
%% Ordner mit Datensätzen
Daten = "Tiefe";

files = dir(fullfile(Daten, "*.txt")); % Dateiliste 

%% Parameter
fs = 100; % Standardwert
Fensterbreite = 2 * fs;   % 2 Sekunden = 200 Samples

%% Trainingsdaten
% X sind alle Eingangsdaten und y sind alle richtigen Antworten
X = []; % die Verschiedenen Signale Matrixmultiplikation 777 Traningsfesnter 200 Messwerte pro Fenster
Y = []; % die richtige Klasse 

%% Alle Dateien prüfen
for k = 1:length(files)

    filename = files(k).name;
    filepath = fullfile(Daten, filename); % durch den Pfad kann MAtlab das öffnen

    disp("Lade Datei: " + filename);

        %% Datei Einlesen
txt = fileread(filepath);
txt = strrep(txt, ',', '.');

tempfile = 'temp_training.txt';
fid = fopen(tempfile, 'w');
fprintf(fid, '%s', txt);
fclose(fid);

data = readmatrix(tempfile, 'Delimiter', ';');

if any(isnan(data(:)))
    error("Fehler: Datei enthält NaN nach dem Einlesen: " + filename);
end

    %% Beschleunigungssignal
    Beschl = data(:,4); % Alle Zeilen aus der Spalte

    %% Offset entfernen
   Beschl = Beschl - mean(Beschl); % Sensoren haben oft offset Verschiebung
   Beschl = normalize(Beschl); % für ähnliche Größen Entfernung von Größenunterschiede

    %% LABEL AUS DATEINAME BESTIMMEN
    if contains(filename, "60BPM", "IgnoreCase", true)

        label = "zu_langsam";

    elseif contains(filename, "100BPM", "IgnoreCase", true)

        label = "korrekt";

    elseif contains(filename, "schnell", "IgnoreCase", true)

        label = "zu_schnell";

         elseif contains(filename, "KeineBewegung")
        label = "Keine_Bewegung";

    else
        warning("Kein Frequenz-Label erkannt für Datei: %s", filename);
        continue;
    end

    %% Signal in 2 Sekunden Fenster schneiden
    Anzahl_Fenster = floor(length(Beschl) / Fensterbreite); % man will wissen wie viele Trainingsfenster es gibt

    for i = 1:Anzahl_Fenster

        Startindex = (i-1) * Fensterbreite + 1;
        Endindex = Startindex + Fensterbreite - 1;

        window = Beschl(Startindex:Endindex);

        X = [X; window'];
        Y = [Y; label];
    end
end

%% Labels umwandeln
Y = categorical(Y);

disp("Größe von X:");
disp(size(X));

disp("Klassen:");
disp(categories(Y));

disp("Anzahl pro Klasse:");
disp(countcats(Y));

%% Modell
layers = [
    featureInputLayer(200)

    fullyConnectedLayer(64)
    reluLayer

    fullyConnectedLayer(32)
    reluLayer

    fullyConnectedLayer(3)

    softmaxLayer
    classificationLayer
];

%% Trainingsoptionen
options = trainingOptions("adam","MaxEpochs",100, "MiniBatchSize",32, "Shuffle","every-epoch","Plots","training-progress","Verbose",false);

%% KI Trainieren
net_freq = trainNetwork(X, Y, layers, options);

%% Modell speichern
save("KI_Frequenzbestimmung.mat", "net_freq");
disp("KI für Herzfrequenz/Frequenz erfolgreich trainiert.");
%% Exportieren des Modells in ONNX
exportONNXNetwork(net_freq, "frequency_model.onnx");

