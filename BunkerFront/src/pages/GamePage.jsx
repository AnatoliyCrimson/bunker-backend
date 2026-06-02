import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import Avatar from "../components/ui/Avatar";
import OverlayingPopup from "../components/uikit/OverlayingPopup";
import { 
    useGetGameStateQuery, 
    useRevealCharacteristicMutation, 
    useVotePlayerMutation, 
    useEndDiscussionMutation,
    useEndStoryMutation,
    useDeleteGameMutation 
} from "../store/api";
import ErrorPage from "./ErrorPage";
import { useSignalR } from "../context/SignalRContext";
import "../styles/pages/Game.scss";

function GamePage() {
    
    const navigate = useNavigate();
    const [selectedPlayerId, setSelectedPlayerId] = useState(null); // текущий игрок
    const [prevSelectedPlayerId, setPrevSelectedPlayerId] = useState(null); // предыдущий для корректного отображения наложения листов
    const timeoutRef = useRef(null);
    const [votedPlayerIds, setVotedPlayerIds] = useState([]);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [timeLeft, setTimeLeft] = useState(null);
    
    const { data: gameState, isLoading, error, refetch } = useGetGameStateQuery(undefined, {
        refetchOnMountOrArgChange: true,
    });
    const { connection, isConnected, startConnection, joinRoom, leaveRoom } = useSignalR();
    
    // 1. Инициализация подключения
    useEffect(() => {
        startConnection();
    }, [startConnection]);

    // 2. Вход в группу комнаты (по roomId из gameState) и прослушивание событий
    useEffect(() => {
        if (!gameState?.roomId || !isConnected || !connection) {
            console.log("SignalR: Waiting for conditions...", { 
                roomId: gameState?.roomId, 
                isConnected, 
                hasConnection: !!connection 
            });
            return;
        }

        console.log(`SignalR: Joining group ${gameState.roomId}`);
        joinRoom(gameState.roomId);

        const onGameUpdated = () => {
            console.log("%c SignalR: GameUpdated received! Refetching...", "color: #4CAF50; font-weight: bold");
            refetch();
        };

        const onGameDeleted = () => {
            console.log("%c SignalR: GameDeleted received!", "color: #F44336; font-weight: bold");
            navigate('/lobby');
        };

        connection.on("GameUpdated", onGameUpdated);
        connection.on("GameDeleted", onGameDeleted);

        return () => {
            console.log(`SignalR: Leaving group ${gameState.roomId} and cleaning up events`);
            connection.off("GameUpdated", onGameUpdated);
            connection.off("GameDeleted", onGameDeleted);
            leaveRoom(gameState.roomId);
        };
    }, [gameState?.roomId, isConnected, connection, joinRoom, leaveRoom, refetch, navigate]);

    // Таймер обсуждения
    useEffect(() => {
        if (gameState?.phase === "Discussion" && gameState?.discussionEndsAt) {
            const timer = setInterval(() => {
                const now = new Date().getTime();
                const end = new Date(gameState.discussionEndsAt).getTime();
                const diff = Math.max(0, Math.floor((end - now) / 1000));
                setTimeLeft(diff);
                if (diff <= 0) clearInterval(timer);
            }, 1000);
            return () => clearInterval(timer);
        } else {
            setTimeLeft(null);
        }
    }, [gameState?.phase, gameState?.discussionEndsAt]);

    const formatTime = (seconds) => {
        if (seconds === null) return "--:--";
        const m = Math.floor(seconds / 60);
        const s = seconds % 60;
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    };

    const [reveal, { isLoading: isRevealing }] = useRevealCharacteristicMutation();
    const [vote, { isLoading: isVoting }] = useVotePlayerMutation();
    const [endDiscussion, { isLoading: isEndingDiscussion }] = useEndDiscussionMutation();
    const [endStory, { isLoading: isEndingStory }] = useEndStoryMutation();
    const [deleteGame, { isLoading: isDeleting }] = useDeleteGameMutation();

    const handlePlayerSelect = (id) => {
        if (id === selectedPlayerId) return;
        if (timeoutRef.current) clearTimeout(timeoutRef.current);

        setPrevSelectedPlayerId(selectedPlayerId);
        setSelectedPlayerId(id);
        
        // Убираем класс prev-active после завершения анимации (800мс)
        timeoutRef.current = setTimeout(() => {
            setPrevSelectedPlayerId(null);
            timeoutRef.current = null;
        }, 500);
    };

    const playersList = gameState?.players || [];
    const me = playersList.find(p => p.isMe);
    const otherPlayers = playersList.filter(p => !p.isMe);
    const currentTurnId = gameState?.currentTurnPlayerId;
    const isHost = gameState?.hostId === me?.userId;

    // Авто-открытие модалки при смене фаз
    useEffect(() => {
        const modalPhases = ["Story", "Discussion", "End"];
        if (gameState?.phase && modalPhases.includes(gameState.phase)) {
            setIsModalOpen(true);
        } else {
            setIsModalOpen(false);
        }
    }, [gameState?.phase]);

    // Установка начального выбранного игрока
    useEffect(() => {
        if (otherPlayers.length > 0 && selectedPlayerId === null) {
            setSelectedPlayerId(otherPlayers[0].id);
        }
    }, [otherPlayers, selectedPlayerId]);

    if (isLoading) return <div className="background game"><div className="game__container">Загрузка игры...</div></div>;
    if (error) return <ErrorPage status={error.status} title="Ошибка" message={error.data} />;

    const selectedPlayer = playersList.find(p => p.id === selectedPlayerId) || otherPlayers[0] || me;

    const handleReveal = async (traitCode) => {
        try {
            await reveal({ traitCode }).unwrap();
        } catch (err) {
            console.error("Не удалось открыть характеристику:", err);
        }
    };

    const handleEndDiscussion = async () => {
        try {
            await endDiscussion().unwrap();
        } catch (err) {
            console.error("Ошибка завершения обсуждения:", err);
        }
    };

    const handleEndStory = async () => {
        try {
            await endStory().unwrap();
        } catch (err) {
            console.error("Ошибка завершения предыстории:", err);
        }
    };

    const handleVoteToggle = (playerId) => {
        const requiredVotes = gameState.availablePlaces - 1;
        if (votedPlayerIds.includes(playerId)) {
            setVotedPlayerIds(votedPlayerIds.filter(id => id !== playerId));
        } else if (votedPlayerIds.length < requiredVotes) {
            setVotedPlayerIds([...votedPlayerIds, playerId]);
        }
    };

    const handleVoteSubmit = async () => {
        try {
            await vote({ targetsPlayerId: votedPlayerIds }).unwrap();
            setVotedPlayerIds([]);
        } catch (err) {
            console.error("Ошибка голосования:", err);
        }
    };

    const handleFinishGame = async () => {
        try {
            await deleteGame().unwrap();
            navigate('/lobby');
        } catch (err) {
            console.error("Ошибка завершения игры:", err);
        }
    };

    return (
        <>
            <div className="background game">
                
                <div className="game__info">
                    <h2 className="game__info-title">{gameState.disasterName}</h2>
                    <p className="game__info-p">{gameState.disasterDescription}</p>    
                    <br />            
                    <h3 className="game__info-title">Ваш бункер:</h3>
                    <p className="game__info-p">{gameState.bunkerDescription}</p>
                    <br />
                    <h3 className="game__info-title">Комнаты:</h3>
                    <ul>
                        {gameState.bunkerRooms?.map((room, idx) => (
                            <li key={idx}>                                
                                <strong>{room.name}</strong> ({room.status}): {room.description}
                                <br />
                            </li>
                        ))}
                    </ul>
                </div>
                
                <div className={`voting ${gameState?.phase === "Voting" ? "voting--active" : ""}`}>
                    <h2 className="voting__title">Голосование</h2>
                    <p>
                        Выберите {gameState.availablePlaces - 1} игроков, которых хотите видеть с собой в бункере                            
                    </p>
                    <ul className="voting__list">
                        {otherPlayers.map(player => (
                            <li key={player.id} className="voting__item">
                                <Avatar avatarUrl={player?.avatarUrl} name={player?.name} className="voting__avatar" />
                                <p className="voting__player-name">{player?.name}</p>
                                <label className="voting__toggle">
                                    <input 
                                        hidden
                                        className="voting__input"
                                        type="checkbox"
                                        checked={votedPlayerIds.includes(player.id)}
                                        onChange={() => handleVoteToggle(player.id)}                                        
                                        disabled={!(gameState?.phase === "Voting" && !(me?.isVoted || (!votedPlayerIds.includes(player.id) && votedPlayerIds.length >= gameState.availablePlaces - 1)))}
                                    />                
                                </label>
                            </li>
                        ))}
                    </ul>
                    <button 
                        disabled={isVoting || me?.isVoted || votedPlayerIds.length !== gameState.availablePlaces - 1}
                        className="btn voting__btn"
                        onClick={handleVoteSubmit}
                    >
                        Проголосовать
                    </button>
                </div>

                // 680 920
                <div className="game__container">
                    <div className="game__book">
                        <div className="players">
                            <div className="players__container">        
                                {otherPlayers.map(player => (                                
                                    <div 
                                        key={player.id}
                                        className={`players__sheet ${selectedPlayerId === player.id ? "players__sheet--active" : ""} ${prevSelectedPlayerId === player.id ? "players__sheet--prev-active" : ""}`}
                                    >
                                        <div                                             
                                            className={`players__mark  ${player.id == currentTurnId ? "players__mark--turn" : ""}`}
                                            onClick={() => handlePlayerSelect(player.id)}
                                        >
                                            <Avatar avatarUrl={player.avatarUrl} name={player.name} className="players__mark-avatar" />
                                            
                                        </div>

                                        <div className="detail">
                                            <h3 className="detail__header">
                                                {player.name}
                                            </h3>
                                            <ul className="cards">
                                                {player.characteristics.map(char => (
                                                    <li key={char.code} className="cards__item">   
                                                        <div className={`cards__container cards__container--${char.code} ${char.isOpen ? "cards__container--active" : ""}`}>
                                                            <div className="cards__front">
                                                                <p className="cards__name">{char.label}</p>
                                                                <div className="cards__front-icon"></div>
                                                            </div>
                                                            <div className="cards__back">
                                                                <div className="cards__back-icon"></div>
                                                                <div className="cards__back-icon"></div>
                                                                <p className="cards__back-name">{char.label}</p>
                                                                <div className="cards__back-value-container">
                                                                    <p className="cards__back-value">{char.value}</p>
                                                                </div>
                                                            </div>                                                           
                                                        </div>
                                                        <div className="cards__angles">
                                                            <div className="cards__angles-item"></div>
                                                            <div className="cards__angles-item"></div>
                                                        </div> 
                                                    </li>
                                                ))}
                                                
                                            </ul>
                                            <p className="detail__score">Голоса: {player.totalScore}</p>
                                        </div>
                                    
                                    </div>                                                            
                                ))}                              
                            </div>

                        </div>
                        <div className="me">
                            <div className="me__container">
                                <div className="me__header">
                                    <div className="me__header-text">
                                        <h3 className="me__header-title">{me.name} </h3>
                                        <p className="me__header-score">Голоса: {me.totalScore}</p>
                                        Этап: {gameState.currentStage}, Раунд: {gameState.currentRound}
                                    </div>
                                    <Avatar avatarUrl={me?.avatarUrl} name={me?.name} className="me__avatar" />
                                </div>
                                <ul>
                                    {me.characteristics.map(char => (
                                        <li key={char.code}>
                                            <div className="characteristics__info">
                                                <p>{char.label}</p>
                                                <p>{char.value}</p>
                                            </div>
                                            {!char.isOpen && (
                                                <button 
                                                    disabled={!gameState.yourTurnNow || isRevealing}
                                                    onClick={() => {handleReveal(char.code)}}
                                                >
                                                    Открыть
                                                </button>
                                            )}
                                        </li>
                                    ))}
                                </ul>                          
                            </div>
                        </div>
                    </div>
                </div>

            </div>

            <OverlayingPopup 
                isOpened={isModalOpen}
                onClose={() => {}}
            >
                <div>
                    {gameState?.phase === "Story" && (
                        <div>
                            <h2>{gameState.disasterName}</h2>
                            <p>{gameState.disasterDescription}</p>
                            <hr />
                            <h3>Ваш бункер</h3>
                            <p>{gameState.bunkerDescription}</p>
                            <ul>
                                {gameState.bunkerRooms?.map((room, idx) => (
                                    <li key={idx}>
                                        <strong>{room.name}</strong> ({room.status}): {room.description}
                                    </li>
                                ))}
                            </ul>
                            {isHost ? (
                                <button disabled={isEndingStory} onClick={handleEndStory}>Понятно</button>
                            ) : (
                                <p>Ожидайте, пока хост завершит просмотр предыстории...</p>
                            )}
                        </div>
                    )}

                    {gameState.phase === "Discussion" && (
                        <div className="discussion">
                            <h2 className="voting__title">Обсуждение</h2>
                            <p className="discussion__timer">Осталось времени: {formatTime(timeLeft)}</p>
                            {isHost && (
                                <button 
                                    disabled={isEndingDiscussion} 
                                    className="btn" 
                                    onClick={handleEndDiscussion}
                                >
                                    Завершить обсуждение
                                </button>
                            )}
                        </div>
                    )}

                    {gameState?.phase === "End" && (
                        <div className="end-game">
                            <h2 className="voting__title">Игра завершена</h2>
                            <p>Мест в бункере: {gameState.availablePlaces}</p>
                            <div className="end-game__winners">
                                {[...playersList]
                                    .sort((a, b) => b.totalScore - a.totalScore)
                                    .slice(0, gameState.availablePlaces)
                                    .map(winner => (
                                        <div key={winner.id} className="end-game__winner">
                                            <Avatar avatarUrl={winner.avatarUrl} name={winner.name} className="game__avatar" />
                                            <p>{winner.name}</p>
                                            <span className="end-game__score">Голосов: {winner.totalScore}</span>
                                        </div>
                                    ))
                                }
                            </div>
                            <hr />
                            <h2>Финальный вердикт</h2>
                            {gameState.achievementVerdict ? (
                                <div>
                                    <div style={{ fontSize: '24px', fontWeight: 'bold' }}>
                                        Успех: {gameState.achievementVerdict.score}/100
                                    </div>
                                    
                                    <section>
                                        <h3>Анализ влияния</h3>
                                        <p>{gameState.achievementVerdict.impactAnalysis}</p>
                                    </section>

                                    <section>
                                        <h3>Хронология выживания</h3>
                                        {gameState.achievementVerdict.survivalTimeline?.map((item, idx) => (
                                            <div key={idx}>
                                                <strong>{item.period}:</strong> {item.event}
                                            </div>
                                        ))}
                                    </section>

                                    <section>
                                        <h3>Альтернативные сценарии</h3>
                                        <div>
                                            <h4>Наилучший (Score: {gameState.achievementVerdict.bestAlternative?.score})</h4>
                                            <p>{gameState.achievementVerdict.bestAlternative?.description}</p>
                                        </div>
                                        <div>
                                            <h4>Наихудший (Score: {gameState.achievementVerdict.worstAlternative?.score})</h4>
                                            <p>{gameState.achievementVerdict.worstAlternative?.description}</p>
                                        </div>
                                    </section>
                                </div>
                            ) : (
                                <p>Генерируем результаты вашего выживания...</p>
                            )}
                            <div className="end-game__actions">
                                {isHost ? (
                                    <button disabled={isDeleting} className="btn" onClick={handleFinishGame}>
                                        Завершить игру
                                    </button>
                                ) : (
                                    <button className="btn" onClick={() => navigate('/lobby')}>
                                        Выйти в лобби
                                    </button>
                                )}
                            </div>
                        </div>
                    )}
                </div>
            </OverlayingPopup>
        </>
    );
}

export default GamePage;
