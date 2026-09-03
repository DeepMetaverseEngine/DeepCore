// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC4610Layout.sol";
import "../erc721/HERC721Logic.sol";
import "./HERC4610Interface.sol";

contract HERC4610Logic is HERC4610Layout, HERC721Logic, HERC4610Interface {

    /**
     * @dev See {IERC4610-setDelegator}.
     */
    function setDelegator(address delegator, uint256 tokenId) public virtual override {
        address owner = ownerOf(tokenId);
        // require(delegator != owner, "ERC4610: setDelegator to current owner");

        require(
            _msgSender() == owner || isApprovedForAll(owner, _msgSender()),
            "ERC4610: setDelegator caller is not owner nor approved for all"
        );

        _setDelegator(delegator, tokenId);
    }

    /**
     * @dev See {IERC4610-delegatorOf}.
     */
    function delegatorOf(uint256 tokenId) public view virtual override returns (address) {
        require(_exists(tokenId), "ERC4610: delegated query for nonexistent token");
        address delegator = _delegators[tokenId];
        return delegator;
    }


    /**
     * @dev See {IERC4610-safeTransferFrom}.
     */
    function safeTransferFrom(
        address from,
        address to,
        uint256 tokenId,
        bool reserved
    ) public virtual override {
        address delegator = delegatorOf(tokenId);
        safeTransferFrom(from, to, tokenId, "");
        if (reserved) _setDelegator(delegator, tokenId);
    }

    /**
     * @dev Set `delegator` as the delegator of `tokenId`.
     *
     * Emits a {SetDelegator} event.
     */
    function _setDelegator(address delegator, uint256 tokenId) internal virtual {
        _delegators[tokenId] = delegator;
        emit SetDelegator(_msgSender(), delegator, tokenId);
    }

    function _burn(uint256 tokenId) virtual override(HERC721Logic) internal {
        HERC721Logic._burn(tokenId);
        // Clear delegators from the previous owner
        _setDelegator(address(0), tokenId);
    }

    function _transfer(address from, address to, uint256 tokenId) virtual override(HERC721Logic) internal {
        HERC721Logic._transfer(from, to, tokenId);
        // Clear delegators from the previous owner
        _setDelegator(address(0), tokenId);
    }
}
