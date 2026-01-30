import { Link, NavLink } from "react-router";
import "../styles/components/Header.scss"

import { useSelector } from "react-redux";
import Avatar from "./ui/Avatar";

function Header() {
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
            <header className="header">
                <div className="header__bottom-line-container">
                    <div className="header__bottom-line">

                    </div>
                
                    <div className="container header__container">
                        {
                            activeSessionData && isAuthenticated && (
                                <>
                                    <div className="session__container">
                                        <h3 className="session__title">Активная сессия</h3>
                                        <p className="session__text">
                                            У вас активная сессия
                                        </p>
                                        <p className="session__info">
                                            нажмите на кнопку что бы вернутся 
                                        </p>
                                        <Link to={activeSessionData.url} className="btn btn--small">
                                            Вернуться
                                        </Link>
                                    </div>
                                </>
                            )
                        }


                        <nav className="nav">                
                            <div className="nav__logo-container">
                                <h1 className="nav__logo logo">
                                    BUNKER
                                </h1>
                            </div>                
                            <ul className="nav__list">
                                <li className="nav__item">
                                    <NavLink to="/rules" className="nav__link">
                                        <div className="nav__item-content">
                                            Правила
                                            <div className="nav__item-active">
                                            </div>
                                        </div>
                                    </NavLink>
                                </li>
                                <li className="nav__item">
                                    <NavLink to="/" className="nav__link">
                                        <div className="nav__item-content">                                        
                                            Играть
                                            <div className="nav__item-active">
                                            </div>
                                        </div>
                                    </NavLink>
                                </li>
                                <li className="nav__item">
                                    <NavLink to="/about-us" className="nav__link">
                                        <div className="nav__item-content">
                                            О нас     
                                            <div className="nav__item-active"></div>                                   
                                        </div>
                                    </NavLink>
                                </li>
                            </ul>
                            <div className="nav__auth">
                                {isAuthenticated && user ?
                                    <>
                                        <NavLink to="/profile" className="nav__profile">
                                            <Avatar 
                                                avatarUrl={user.avatarUrl}
                                                name={user.name}
                                                className={"nav__avatar-container"}
                                            />
                                            {/* <img src={"http://localhost:5135" + user.avatarUrl} height="50px" width="50px" alt="" /> */}
                                            <span>
                                                {user.name}
                                            </span>
                                        </NavLink>
                                    </> 
                                :
                                    <>
                                        <NavLink to="/auth" className="nav__btn">
                                            Войти
                                        </NavLink>
                                        {/* <NavLink to="/auth/registration" className="nav__btn">
                                            Регистрация
                                        </NavLink>                                 */}
                                    </>
                                }
                            </div>
                        </nav>
                    </div>
                </div>
            </header>
        </> 
    );
}

export default Header;