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
                url: `/game`,
                type: "игровая сессия"
            };
        }
        
        if (user.currentRoomId) {
            return {
                url: `/room`,
                type: "комната ожидания"
            };
        }

        return null;
    })();



    return (
        <>
            <header className="header">
                <nav className="nav">
                    <img className="chain nav__chain" src="/src/assets/model-chain.png" alt="" />
                    <img className="chain nav__chain" src="/src/assets/model-chain.png" alt="" />
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
                                </div>
                            </NavLink>
                        </li>
                        <li className="nav__item">
                            <NavLink to="/" className="nav__link">
                                <div className="nav__item-content">                                        
                                    Играть                                
                                </div>
                            </NavLink>
                        </li>
                        <li className="nav__item">
                            <NavLink to="/about-us" className="nav__link">
                                <div className="nav__item-content">
                                    О нас                                                                        
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
                            </>
                        }
                    </div>
                </nav>
            </header>




        
        </> 
    );
}

export default Header;