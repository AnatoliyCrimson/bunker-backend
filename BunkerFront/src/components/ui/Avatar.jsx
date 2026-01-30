import "../../styles/components/ui/Avatar.scss"

function Avatar({ avatarUrl, name, className, alt = "Avatar" }) {
    

    return (
        <>
            <div className={"avatar " + className}>                
                {avatarUrl ? (
                    <>
                        <img
                            key={avatarUrl}
                            src={"http://localhost:5135" + avatarUrl}
                            alt={alt}
                            className='avatar__img' 
                        />
                    </>
                ) : (
                    <>
                        <div className="avatar__empty">
                            {name ? name[0].toUpperCase() : "?"}
                        </div>
                    </>
                )}


            </div>
            
        </>
    );
    
}

export default Avatar;