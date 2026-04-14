import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import Avatar from "../components/ui/Avatar";
import OverlayingPopup from "../components/uikit/OverlayingPopup";
import { 
    useGetGameStateQuery, 
    useRevealCharacteristicMutation, 
    useVotePlayerMutation, 
    useEndDiscussionMutation,
    useDeleteGameMutation 
} from "../store/api";
import ErrorPage from "./ErrorPage";
import "../styles/pages/Game.scss";

function GamePage() {
    const navigate = useNavigate();
    const [selectedPlayerId, setSelectedPlayerId] = useState(null);
    const [votedPlayerIds, setVotedPlayerIds] = useState([]);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [timeLeft, setTimeLeft] = useState(null);

    const { data: gameState, isLoading, error } = useGetGameStateQuery(undefined, {
        pollingInterval: 2000,
        refetchOnMountOrArgChange: true,
    });

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
    const [deleteGame, { isLoading: isDeleting }] = useDeleteGameMutation();

    const playersList = gameState?.players || [];
    const me = playersList.find(p => p.isMe);
    const otherPlayers = playersList.filter(p => !p.isMe);
    const currentTurnId = gameState?.currentTurnPlayerId;
    const isHost = gameState?.hostId === me?.userId;

    // Авто-открытие модалки при смене фазы на Discussion или Voting
    useEffect(() => {
        if (gameState?.phase === "Discussion" || gameState?.phase === "Voting") {
            setIsModalOpen(true);
        } else if (gameState?.phase === "End") {
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
                <div className="game__container">
                    <div className="game__book">
                        <div className="players">
                            <div className="players__container">        
                                {otherPlayers.map(player => (
                                    <>
                                        <div 
                                            key={player.id}
                                            className={`players__sheet ${selectedPlayerId === player.id ? "players__sheet--active" : ""}`}
                                        >
                                            <div                                             
                                                className={`players__mark  ${player.id == currentTurnId ? "players__mark--turn" : ""}`}
                                                onClick={() => setSelectedPlayerId(player.id)}
                                            >
                                                <Avatar avatarUrl={player.avatarUrl} name={player.name} className="players__mark-avatar" />
                                                
                                            </div>

                                            <div className="detail">
                                                <h3 className="detail__header">
                                                    {player.name}
                                                </h3>
                                                <ul className="cards">
                                                    {player.characteristics.map(char => (
                                                        <li 
                                                            key={char.code}
                                                            className={`cards__item cards__item--${char.code} ${char.isOpen ? "cards__item--active" : ""}`}
                                                        >   
                                                            <div className="cards__front">
                                                                <p className="cards__name">{char.label}</p>
                                                            </div>
                                                            <div className="cards__back">
                                                                <div className="cards__back-icon"></div>
                                                                <div className="cards__back-icon"></div>
                                                                <p className="cards__name">{char.label}</p>
                                                                <p className="cards__value">{char.value}</p>
                                                            </div>
                                                        </li>
                                                    ))}
                                                    
                                                </ul>
                                                <p className="detail__score">Голоса: {player.totalScore}</p>
                                            </div>
                                        
                                        </div>                            
                                    </>
                                ))}                              
                            </div>

                        </div>
                        <div className="me">

                        </div>
                    </div>
                </div>

            </div>
        </>
    );
}

export default GamePage;
