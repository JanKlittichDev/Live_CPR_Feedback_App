clear
clc

%% Ordner mit Datensätzen
folder = "Tiefe";

files = dir(fullfile(folder, "*.txt"));

%% Parameter
fs = 100;                 % Abtastrate
windowSize = 2 * fs;      % 2 Sekunden = 200 Samples

%% Trainingsdaten
X = [];
Y = [];

%% Alle Dateien durchgehen
for k = 1:length(files)
    
    filename = files(k).name;

    filepath = fullfile(folder, filename);

    disp("Lade Datei: " + filename);

    %% Datei einlesen
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
    acceleration = data(:,4);

    %% Offset Entfernen
    acceleration = acceleration - mean(acceleration);

    %% Label aus daten bestimmen

    if contains(filename, "zuFlach")

        label = "zu_flach";

    elseif contains(filename, "korrekt")

        label = "korrekt";

    elseif contains(filename, "zuTief")

        label = "zu_tief";

    elseif contains(filename, "KeineBewegung")
        label = "Keine_Bewegung";

    else
        continue;
    end

    
    numWindows = floor(length(acc) / windowSize);

    %% Signal in 2 Sekunden Fenster zerschneiden
    for i = 1:numWindows

        idx1 = (i-1) * windowSize + 1;
        idx2 = idx1 + windowSize - 1;

        %% 2s SIGNAL
        window = acceleration(idx1:idx2);

        %% TRAININGSDATEN SPEICHERN
        X = [X; window'];

        Y = [Y; label];
    end
end

%% Kategorien
Y = categorical(Y);

%% Klassen Anzeigen
disp(categories(Y));

%% Netzwerkarchitektur
layers = [

    featureInputLayer(200)

    fullyConnectedLayer(64)
    reluLayer

    fullyConnectedLayer(32)
    reluLayer

    fullyConnectedLayer(4)

    softmaxLayer
    classificationLayer
];

%% Trainingsoptionen
options = trainingOptions("adam", "MaxEpochs",100, "MiniBatchSize",32,"Shuffle","every-epoch","Plots","training-progress","Verbose",false);

%% Training
net_depth = trainNetwork(X, Y, layers, options);

%% Modell speichern
save("KI_Tiefe.mat", "net_depth");
disp("KI für Tiefenbestimmung erfolgreich trainiert.");

%% KI Exportieren in die App
exportONNXNetwork(net_depth, "depth_model.onnx");