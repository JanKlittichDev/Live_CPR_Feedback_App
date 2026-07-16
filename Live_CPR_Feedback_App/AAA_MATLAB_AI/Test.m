%% Ki Test
clear; clc; close all;

%% KIs laden
load("KI_Tiefe.mat", "net_depth");
load("KI_Frequenzbestimmung.mat", "net_freq");

%% Testdatei
% Hier Pfad eintragen für die Datei
testfile = "";


%% Parameter
fs = 100;
Fensterbreite = 2 * fs;

%% Datei einlesen
txt = fileread(testfile);

txt = strrep(txt, ',', '.');

fid = fopen('temp.txt','w');
fprintf(fid,'%s',txt);
fclose(fid);

data = readmatrix('temp.txt','Delimiter',';');

%% Beschleunigung

acc = data(:,4);

%% Gleiche Vorverarbeitung wie beim Training

acc = acc - mean(acc);

%% Fenster schneiden

numWindows = floor(length(acc) / Fensterbreite);

X_test = [];

for i = 1:numWindows

idx1 = (i-1) * Fensterbreite + 1;

idx2 = idx1 + Fensterbreite - 1;

window = acc(idx1:idx2);

X_test = [X_test; window'];

end

%% Tiefen Ki
YPred_depth = classify(net_depth, X_test);

%% Frequenz Ki
YPred_freq = classify(net_freq, X_test);

%% Labels festlegen(Für die gesamte Datei)
% 1. Kategorien definieren exakt so wie die KI es gelernt hat
klassen_tiefe = {'zu_flach', 'korrekt', 'zu_tief'};
klassen_rate  = {'zu_langsam', 'korrekt', 'zu_schnell'};

% 2. True-Label-Array in der Länge der Fensteranzahl erstellen
Y_true_depth = repmat({'zu_tief'}, numWindows, 1);
Y_true_freq  = repmat({'korrekt'}, numWindows, 1);

% In Categorical mit festen Kategorien umwandeln 
Y_true_depth = categorical(Y_true_depth, klassen_tiefe);
Y_true_freq  = categorical(Y_true_freq, klassen_rate);

%% Berechnung der Wahrscheinlichkeit
% Vergleicht Vorhersage mit wahrem Label und berechnet den Prozentwert
acc_depth = sum(YPred_depth == Y_true_depth) / numWindows * 100;
acc_freq  = sum(YPred_freq == Y_true_freq) / numWindows * 100;

%% Konsolenausgabe
disp("===== ERGEBNISSE DER KLASSIFIZIERUNG =====")
fprintf("Tiefe (Mehrheitsentscheid): %s\n", string(mode(YPred_depth)));
fprintf("Rate  (Mehrheitsentscheid): %s\n", string(mode(YPred_freq)));
fprintf("------------------------------------------\n");
fprintf("Genauigkeit Tiefen-KI: %.1f%%\n", acc_depth);
fprintf("Genauigkeit Raten-KI:  %.1f%%\n", acc_freq);
disp("==========================================")

%% Confusion Chart
% Erstelle ein Fenster mit definiertem Namen und passender Breite
figure('Name', 'Ergebnisse der KI-Klassifizierung', 'NumberTitle', 'off', 'Position', [100, 100, 900, 450]);

% Layout für zwei nebeneinander liegende Plots
tiledlayout(1, 2, 'TileSpacing', 'compact');

% Tiefen KI
nexttile;
cm_depth = confusionchart(Y_true_depth, YPred_depth);
% Titel mit dynamischer Prozentzahl
cm_depth.Title = sprintf('Tiefen-KI (%.0f%%)', acc_depth);
% Zeigt die Prozentzahlen am rechten Rand 
cm_depth.RowSummary = 'row-normalized'; 

% Rate KI
nexttile;
cm_freq = confusionchart(Y_true_freq, YPred_freq);
cm_freq.Title = sprintf('Raten-KI (%.0f%%)', acc_freq);
cm_freq.RowSummary = 'row-normalized';

%% Temporäre Datei löschen
if isfile('temp.txt')
    delete('temp.txt');
end