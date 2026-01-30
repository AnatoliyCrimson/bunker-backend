import { useNavigate, useParams } from "react-router";
import Avatar from "../components/ui/Avatar";
import "../styles/pages/Game.scss"
import { useGetGameStateQuery, useRevealCharacteristicMutation, useVotePlayerMutation, useDeleteGameMutation, } from "../store/api";
import ErrorPage from "./ErrorPage";
import { useEffect, useState } from "react";

function Game() {

    const { id } = useParams();

    const [selectedPlayerId, setSelectedPlayerId] = useState(null);
    const [voteTargetId, setVoteTargetId] = useState(null);
    const navigate = useNavigate();

    const { data: gameState, isLoading, error } = useGetGameStateQuery(id, {
        refetchOnMountOrArgChange: true,
        pollingInterval: 2000, // shortPulling
    })

    const [reveal, { isLoading: isRevealing }] = useRevealCharacteristicMutation();
    const [vote, { isLoading: isVoting }] = useVotePlayerMutation();
    const [deleteGame, { isLoading: isDeleting }] = useDeleteGameMutation();
    
    const playersList = gameState?.players || [];
    
    
    const me = playersList.find(p => p.isMe);
    const otherPlayers = playersList.filter(p => !p.isMe);
    
    const currentTurnId = gameState?.currentTurnPlayerId;
    
    useEffect(() => {
        if (otherPlayers.length > 0 && selectedPlayerId === null) {
            setSelectedPlayerId(otherPlayers[0].id);
        }
    }, [otherPlayers, selectedPlayerId]);
    
    const handleReveal = async (code) => {
        try {
            await reveal({ 
                gameId: id, 
                traitName: code 
            }).unwrap();
        } catch (err) {
            console.error("Не удалось открыть характеристику:", err);
        }
    };

    const handleVote = async () => {
        if (!voteTargetId) return;

        try {
            await vote({ 
                gameId: id, 
                targetPlayerId: voteTargetId 
            }).unwrap();
            
            // Сбрасываем выбор после успешного голосования (опционально)
            setVoteTargetId(null);
            
            // Можно вывести уведомление "Голос принят"
        } catch (err) {
            console.error("Ошибка голосования:", err);
            // alert(err.data?.message || "Не удалось проголосовать");
        }
    };

    const handleFinishGame = async () => {
        try {
            await deleteGame(id).unwrap();
            // После успешного удаления перекидываем в лобби
            navigate('/lobby');
        } catch (err) {
            console.error("Ошибка при завершении игры:", err);
            // alert("Не удалось завершить игру");
        }
    };

    if (isLoading) return <div className="background game"><div className="game__container" style={{display:'flex', justifyContent:'center', alignItems:'center'}}>Загрузка игры...</div></div>;
    
    if (error) {
        return (
            <ErrorPage
                status={error.status || "Ошибка"}
                title={error.status === 404 ? "Бункер не найден" : "Ошибка загрузки"}
                message={error.status === 404 ? 
                    "Скорее всего счастливчики уже в бункере, а остальных поглотил беспощадный мир" :
                    "Ошибка связи с сервером"
                }
            />
        )
    }

    console.log(me)

    const selectedPlayer = gameState.players.find(p => p.id === selectedPlayerId) || otherPlayers[0];

    const winners = gameState.players.filter(p => gameState.winnerIds?.includes(p.id));
    const hasWinners = winners.length > 0;
    const isHost = gameState.hostId === me.userId
    
    return (
        <>
            <div className="background game">
                <div className="game__container">
                    {
                        hasWinners &&
                        <>
                            <div className={`modal ${hasWinners ? "modal--active" : ""}`}>
                                <div className="modal__content">
                                    <h2 className="modal__title">
                                        Счастливчики прошедшие в бункер
                                    </h2>
                                    <div className="modal__list">
                                        {winners.map(winner => (
                                            <div key={winner.id} className="modal__winner-item">
                                                <Avatar 
                                                    avatarUrl={winner.avatarUrl}
                                                    name={winner.name}
                                                    className="players__avatar modal__avatar" // Используем те же стили, что и в игре
                                                />
                                                <p className="modal__winner-name">
                                                    {winner.name}
                                                </p>
                                            </div>
                                        ))}
                                    </div>
                                    {
                                        isHost &&
                                        <button disabled={isDeleting} onClick={handleFinishGame} className="btn modal__btn">{isDeleting ? "Завершение..." : "Завершить игру"}</button>

                                    }
                                </div>
                            </div>
                        </>

                    }
                    <div className="players bg-block">
                        <ul className="players__list">
                            {
                                otherPlayers.map(player => {
                                    const isTurn = player.id === currentTurnId;
                                    const isActive = player.id === selectedPlayer?.id; 

                                    return (
                                        <li 
                                            key={player.id}
                                            className={`players__item ${isActive ? "players__item--active" : ""} ${isTurn ? "players__item--turn" : ""}`}
                                            onClick={() => setSelectedPlayerId(player.id)}
                                        >
                                            <Avatar 
                                                avatarUrl={player.avatarUrl}
                                                name={player.name}
                                                className={"players__avatar game__avatar"}
                                            />
                                            <p className="players__name">
                                                {player.name}
                                            </p>
                                        </li>              

                                    )
                                })

                            }

                        </ul>
                    </div>
                    <div className="specifications bg-block">
                        <h3 className="specifications__title">
                            Характеристики {selectedPlayer.name}
                        </h3>
                        {selectedPlayer ? (
                            <>
                                <ul className="specifications__list">
                                    {
                                        selectedPlayer.characteristics.map((char) => (
                                            <li key={char.code} className="specifications__item">
                                                <div className="specifications__info">
                                                    <p className="specifications__name">
                                                        {char.label}
                                                    </p>
                                                    <p className="specifications__value">
                                                        {char.value}
                                                    </p>
                                                </div>
                                            </li>
                                        ))
                                    }
                                </ul>
                            </>
                        ) : (
                            <>
                                <p className="specifications__empty">Ожидание игроков...</p>
                            </>
                        )}
                        
                    </div>
                    <div className="voting bg-block">
                        <h3 className={`voting__title ${gameState.phase === "Voting" ? "voting__title--active" : ""}`}>
                            Голосование
                        </h3>
                        <div className="voting__content">
                            <ul className="voting__list">
                                {gameState.players.map(player => (
                                    <>
                                        <li key={player.id} className="voting__item">
                                            <div className="voting__player-info">
                                                <span className="voting__player-name">
                                                    {player.name}
                                                </span>
                                                <span className="voting__player-count">
                                                    {player.totalScore} {player.isMe && <span style={{opacity: 0.6}}>(Вы)</span>}
                                                </span>
                                            </div>
                                            {!player.isMe &&
                                                <input
                                                    type="radio"
                                                    name="votingGroup"
                                                    disabled={gameState.phase !== "Voting" || isVoting}
                                                    checked={voteTargetId === player.id}
                                                    onChange={() => setVoteTargetId(player.id)}
                                                /> 
                                            }
                                             
                                        </li>
                                    </>
                                ))}
                            </ul>
                            <button
                                disabled={gameState.phase !== "Voting" || isVoting || !voteTargetId}
                                className="voting__btn btn btn--small"
                                onClick={handleVote}
                            >Голосовать</button>
                        </div>
                    </div>
                    <div className="me bg-block">
                        <div className="me__info">
                            <Avatar 
                                avatarUrl={me.avatarUrl}
                                name={me.name} // сюда нужно передавать имя игрока, что бы если не было url аватара вместо аватара показывалась первая буква имени
                                className={"me__avatar game__avatar"}
                            />
                            <h3 className="me__title">
                                Вы
                            </h3>
                        </div>
                        <ul className="specifications__list">
                            {
                                me.characteristics.map((characteristic) => (
                                    <li className="specifications__item">
                                        <div className="specifications__info">
                                            <p className="specifications__name">
                                                {characteristic.label}
                                            </p>
                                            <p className="specifications__value">
                                                {characteristic.value}
                                            </p>
                                        </div>
                                        {
                                            !characteristic.isOpen && (
                                                <button
                                                    disabled={!gameState.yourTurnNow || isRevealing}
                                                    className="btn btn--small"
                                                    onClick={() => handleReveal(characteristic.code)}
                                                >Открыть</button>
                                            )       
                                        }
                                    </li>
                                ))
                            }


                        </ul>
                    </div>
                </div>
            </div>
        </>
    );
}

export default Game;