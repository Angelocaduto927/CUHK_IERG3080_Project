# CUHK_IERG3080_2025_fall_Final_Project  

This repository contains the code and documentation for the final project of the CUHK IERG3080 course in Fall 2025. The project focuses on designing and implementing a simplified Taiko no Tatsujin game.  

We adopt the MVVM (Model-View-ViewModel) architectural pattern to ensure a clear separation of concerns and enhance maintainability. The project is developed using C# and WPF (Windows Presentation Foundation) in Visual Studio for the user interface.  

## Game Demo
![Game Demo](./game_demo.mp4)

## Project Structure
- **Model**: Contains the core game logic, including classes for game mechanics, scoring, and note management.  
- **View**: Comprises the XAML files that define the user interface layout and visual elements.  
- **ViewModel**: Acts as an intermediary between the Model and View, handling data binding and user interactions.
## Getting Started
To run the project, follow these steps:
1. Clone the repository:  
   `git clone https://github.com/Angelocaduto927/CUHK_IERG3080_Project.git`
2. Open the solution file in Visual Studio.
3. Build the solution to restore dependencies.
4. Run the application. 
## Features Requirements
- Basic Features:
    - A title screen, asking the user to start the game 
    - A song selection screen. Only one song is required in this project, but you are 
welcome to include more. 
    - The in-game screen. You need to show the taiko drum notes that scroll from the right 
to the left. Also, you need to play a song (any song you decide). The score sheet (the 
correct timing to strike the drum and color of the notes) for your selected song is 
designed by you, but there must include a few red and blue notes for testing. The 
speed of scrolling is also decided by you. 
    - Striking the correct key (you can hardcode the control settings) for the note in time 
results in a “perfect” score for this note. Striking a little bit earlier or sooner results in 
a “good” score. Striking more earlier or sooner is considered a “bad” strike. Beyond 
this time interval, the strike has no effect on this note. You need to notify the player 
how good the timing of the strike is, i.e., showing the words “perfect”, “good” and 
“bad”. You can design your own formula to compute the score. 
    - Striking the drum plays a sound effect, which is independent of the notes sliding on 
the screen. Red and blue strikes have a different sound effect. 
    - A game over screen. Once the song is finished (and all notes have been slided out 
the screen), the game is finished. The performance of the player is concluded, 
including the score, the number of “perfect”, “good” and “bad” strikes, etc. Then, the 
player can go back to the song selection screen to start another new game. 

- Advanced Features1:
    - Implement a local 2-player game mode. In the in-game screen, duplicate the band for scrolling the notes, one for each player. Although the game is playing the same song, the score sheet can be the same or different (depends on your design). Scores are omputed separately. 
    - Implement a difficulty selection screen after song selection. Each player can select his/her own difficulty. A different difficulty corresponds to a different score sheet for 
the same song. At least 2 difficulties are required. 
    - Implement game settings for the players. Each player can configure the keys for striking red and blue drums (2 keys per color). Also, each player can select the multiplier for the note scrolling speed (say, 1x, 2x, 3x and 4x).
- Advanced Features2:
    - You need to support both local and online 2-player mode. You need to finish all the requirements described in Feature 1. On top of this, you need to implement the screen for connecting the players. Say, one player hosts the server and waits for the other player to connect. The other player inputs the IP address of the host to join the game session. Once the connection is established, the game enters the song selection screen. You need to handle errors, e.g., disconnection during gameplay. Both players can see the “same” in-game screen on their own machine, i.e., the player can see the strikes (the strike timing words, sound effect, etc.) of another player just like a local 2-player mode.

## License
This project is licensed under a custom non-commercial, non-academic-use license.
See [LICENSE](LICENSE) for details.

## 🙏 Acknowledgements

Thanks to the instructors and teaching team of **IERG3080** at The Chinese University of Hong Kong for their inspiring course on **Software Engineering** . This project is a personal study effort based on concepts learned in the course and online learning materials.

Also, I sincerely thank my parter Jerome and Kaixin for their collaboration and support throughout the development of this project.

❗ This code is for **personal learning only** — **not for homework submission or commercial use**.