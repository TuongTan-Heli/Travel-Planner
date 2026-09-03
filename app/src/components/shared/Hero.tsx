import React from 'react';
import '../../styles/Hero.css';

export default function Hero(): JSX.Element {
    return (
        <section className="hero">
            <div className="hero-inner">
                <div className="hero-left">
                    <h1 className="hero-title">Travel Planner</h1>
                    <p className="hero-sub">Small SPA with itinerary planning features.</p>
                    <div className="hero-rows-md">
                        <div className="row instruction">Provide your travel details to get started!</div>

                        <div className="row">
                            <div className="col">
                                <div className="feature-label">Using our chat</div>
                                <img src="/assets/Chat.png" alt="Chat" />
                            </div>
                            <div className="col">
                                <div className="feature-label">Or Planner</div>
                                <img src="/assets/Planner.png" alt="Planner" />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col">
                                <img src="/assets/Map.png" alt="Map" />
                                <div className="feature-label">Explore the map</div>
                            </div>
                            <div className="col">
                                <img src="/assets/DayPlan.png" alt="Day Plan" />
                                <div className="feature-label">Select your stops</div>
                            </div>
                        </div>
                    </div>
                    <div className="hero-rows-sm">
                        <div className="row instruction">Provide your travel details to get started!</div>
                        <div className="row">
                            <div className="col">
                                <div className="feature-label">Using our chat</div>
                                <img src="/assets/Chat.png" alt="Chat" />
                            </div>
                            <div className="col">
                                <div className="feature-label">Or Planner</div>
                                <img src="/assets/Planner.png" alt="Planner" />
                            </div>
                       

                            <div className="col">
                                <img src="/assets/Map.png" alt="Map" />
                                <div className="feature-label">Explore the map</div>
                            </div>
                            <div className="col">
                                <img src="/assets/DayPlan.png" alt="Day Plan" />
                                <div className="feature-label">Select your stops</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}