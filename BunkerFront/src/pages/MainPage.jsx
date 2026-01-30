import "../styles/pages/Play.scss";
import { Link } from "react-router";
import { useSelector } from "react-redux";
import { useState } from "react";

function MainPage() {


    const {isAuthenticated, user} = useSelector((state) => state.auth)
    
    const activeSessionData = (() => {
        if (!user) return null;

        if (user.currentGameId) {
            return {
                url: `/game/${user.currentGameId}`,
                type: "игровая сессия"
            };
        }
        
        if (user.currentRoomId) {
            return {
                url: `/room/${user.currentRoomId}`,
                type: "комната ожидания"
            };
        }

        return null;
    })();


    return (
        <>
            <div className="background background--main play">
                <div className="container play__container">
                    <h1 className="play__title logo">
                        BUNKER
                    </h1>
                    {
                        activeSessionData ? (
                            <>
                                <Link to={activeSessionData.url} className="btn">Вернуться в игру</Link>
                            </>
                        ) : (
                            <>
                                <Link to="/lobby" className="btn">Н А Ч А Т Ь&nbsp;&nbsp;&nbsp;И Г Р У</Link>
                            </>
                        )

                    }
                </div>
            </div>
        </>
    );
}

export default MainPage;